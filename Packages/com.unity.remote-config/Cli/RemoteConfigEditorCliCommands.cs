#if UNITY_PIPELINE_AVAILABLE && UNITY_EDITOR && DEPLOYMENT_API_AVAILABLE_V1_0
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Unity.Pipeline.Commands;
using Unity.Pipeline.Editor.Authoring;
using RemoteConfigJsonConverter = Unity.Services.RemoteConfig.Authoring.Editor.Json.JsonConverter;
using Unity.Services.RemoteConfig.Editor.Authoring.Core.Formatting;
using Unity.Services.RemoteConfig.Editor.Authoring.Core.Model;
using UnityEditor;

namespace Unity.Services.RemoteConfig.Cli
{
    static class RemoteConfigEditorCliCommands
    {
        const string k_DefaultName = "remote_config";
        const string k_Extension = ".rc";

        [CliGroupCommand(RemoteConfigCliConstants.Group,
            "Create a Remote Config deployment file (.rc) holding config entries for one environment. "
            + "Deploy it with `services.deployment deploy_items`; it is listed by `services.deployment list_items`.",
            Tags = new[] { RemoteConfigCliConstants.Service, RemoteConfigCliConstants.RemoteConfig })]
        public static CreatedRemoteConfigDto CreateConfig(
            [CliArg("path", "Project-relative asset path (the Assets/ prefix and .rc extension are optional). Defaults to Assets/remote_config.rc. An existing file is never overwritten: the new file gets a numbered name, returned in Path")] string path = null,
            [CliArg("entries", "JSON object of config key to value, e.g. '{\"turn_timer\":30,\"achievements\":[{\"id\":\"first_win\"}]}'. Omit for an empty file.")] string entries = null,
            [CliArg("types", "JSON object overriding the inferred type per key, e.g. '{\"payout\":\"FLOAT\"}'. Valid: STRING, INT, BOOL, FLOAT, LONG, JSON. INT must be a whole number within 32-bit range, LONG a whole number within 64-bit range. Only needed when a whole number means LONG or FLOAT - everything else is inferred from the value.")] string types = null)
        {
            var parsedEntries = ParseEntries(entries);
            var declared = ParseTypes(types, parsedEntries);
            var content = ToFileContent(parsedEntries, declared);
            var assetPath = PrepareConfigPath(path, k_DefaultName, k_Extension);

            // The package's own .rc writer, as the Deployment window's fetch uses.
            File.WriteAllText(Path.GetFullPath(assetPath), new RemoteConfigJsonConverter().SerializeObject(content));
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);

            return new CreatedRemoteConfigDto
            {
                Type = "RemoteConfigFile",
                Path = assetPath,
                Keys = parsedEntries.Properties().Select(property => property.Name).ToList(),
                Types = content.types.ToDictionary(pair => pair.Key, pair => pair.Value.ToString()),
            };
        }

        static JObject ParseEntries(string entries)
        {
            if (string.IsNullOrWhiteSpace(entries))
                return new JObject();

            var parsed = ParseJson(entries, "--entries");
            var asObject = parsed as JObject;
            if (asObject == null)
                throw new ArgumentException($"--entries must be a JSON object of key to value, not {Describe(parsed)}.");

            foreach (var property in asObject.Properties())
            {
                if (string.IsNullOrWhiteSpace(property.Name))
                    throw new ArgumentException("--entries contains an empty config key.");
                if (property.Value.Type == JTokenType.Null)
                    throw new ArgumentException($"Config key '{property.Name}' has a null value. Remote Config has no null type - give it a value or drop the key.");
            }
            return asObject;
        }

        // DateParseHandling.None: a string that looks like a timestamp must stay the exact string the
        // caller passed. The default turns it into a Date token, which is written back reformatted
        // (offsets especially) and which Remote Config has no type for.
        static JToken ParseJson(string json, string argument)
        {
            try
            {
                using (var reader = new JsonTextReader(new StringReader(json)) { DateParseHandling = DateParseHandling.None })
                {
                    var parsed = JToken.ReadFrom(reader);
                    if (reader.Read())
                        throw new JsonReaderException("Additional text found after the end of the JSON value.");
                    return parsed;
                }
            }
            catch (JsonReaderException exception)
            {
                throw new ArgumentException($"{argument} is not valid JSON: {exception.Message}");
            }
        }

        static Dictionary<string, ConfigType> ParseTypes(string types, JObject entries)
        {
            var declared = new Dictionary<string, ConfigType>(StringComparer.Ordinal);
            if (string.IsNullOrWhiteSpace(types))
                return declared;

            var parsed = ParseJson(types, "--types");
            var asObject = parsed as JObject;
            if (asObject == null)
                throw new ArgumentException($"--types must be a JSON object of key to type name, not {Describe(parsed)}.");

            foreach (var property in asObject.Properties())
            {
                ConfigType declaredType;
                if (!TryParseType(property.Value, out declaredType))
                    throw new ArgumentException($"'{property.Value}' is not a valid type for key '{property.Name}'. Valid values: {string.Join(", ", Enum.GetNames(typeof(ConfigType)))}.");

                var value = entries[property.Name];
                if (value == null)
                    throw new ArgumentException($"--types names key '{property.Name}', which is not in --entries. Type overrides only apply to keys the file actually holds.");

                if (!Compatible(declaredType, value))
                    throw new ArgumentException($"Key '{property.Name}' is declared {declaredType} but its value is {Describe(value)}, which reads as {Infer(value)}.");

                declared[property.Name] = declaredType;
            }
            return declared;
        }

        // By name only, ignoring case: Enum.TryParse alone would also take "3" or "1,2".
        static bool TryParseType(JToken token, out ConfigType type)
        {
            type = default;
            if (token.Type != JTokenType.String)
                return false;

            var name = token.Value<string>().Trim();
            var match = Enum.GetNames(typeof(ConfigType))
                .FirstOrDefault(candidate => string.Equals(candidate, name, StringComparison.OrdinalIgnoreCase));
            if (match == null)
                return false;

            type = (ConfigType)Enum.Parse(typeof(ConfigType), match);
            return true;
        }

        // STRING, INT, BOOL and JSON are unambiguous in the JSON value, so - as `ugs remote-config
        // new-file` and the package's fetch do - only the types a reader could not recover, plus any the
        // caller declared, are written to `types`.
        static RemoteConfigFileContent ToFileContent(JObject entries, Dictionary<string, ConfigType> declared)
        {
            var content = new RemoteConfigFileContent();
            foreach (var property in entries.Properties())
            {
                content.entries[property.Name] = property.Value;

                ConfigType type;
                if (declared.TryGetValue(property.Name, out type))
                {
                    content.types[property.Name] = type;
                    continue;
                }

                type = Infer(property.Value);
                // An undeclared whole number too big for 64 bits would infer LONG and only fail at deploy.
                if (type == ConfigType.LONG && !FitsLong(property.Value))
                    throw new ArgumentException($"Key '{property.Name}' is {property.Value}, which is out of range for LONG (64-bit). Remote Config has no larger whole-number type; use a STRING or FLOAT instead.");
                if (type == ConfigType.LONG || type == ConfigType.FLOAT)
                    content.types[property.Name] = type;
            }
            return content;
        }

        static ConfigType Infer(JToken value)
        {
            switch (value.Type)
            {
                case JTokenType.Boolean:
                    return ConfigType.BOOL;
                case JTokenType.Float:
                    return ConfigType.FLOAT;
                case JTokenType.Integer:
                    return FitsInt(value) ? ConfigType.INT : ConfigType.LONG;
                case JTokenType.Object:
                case JTokenType.Array:
                    return ConfigType.JSON;
                default:
                    return ConfigType.STRING;
            }
        }

        // A JSON integer is read as a long, or as a BigInteger when it does not fit one.
        static bool FitsLong(JToken value)
        {
            return ((JValue)value).Value is long;
        }

        static bool FitsInt(JToken value)
        {
            var raw = ((JValue)value).Value;
            return raw is long && (long)raw >= int.MinValue && (long)raw <= int.MaxValue;
        }

        static bool Compatible(ConfigType declaredType, JToken value)
        {
            switch (declaredType)
            {
                case ConfigType.STRING:
                    return value.Type == JTokenType.String;
                case ConfigType.BOOL:
                    return value.Type == JTokenType.Boolean;
                // INT and LONG take whole numbers in range only: the deployed value would otherwise be
                // truncated or wrapped without any error.
                case ConfigType.INT:
                    return value.Type == JTokenType.Integer && FitsInt(value);
                case ConfigType.LONG:
                    return value.Type == JTokenType.Integer && FitsLong(value);
                case ConfigType.FLOAT:
                    return value.Type == JTokenType.Integer || value.Type == JTokenType.Float;
                case ConfigType.JSON:
                    return value.Type == JTokenType.Object || value.Type == JTokenType.Array;
                default:
                    return false;
            }
        }

        static string Describe(JToken value)
        {
            switch (value.Type)
            {
                case JTokenType.String:
                    return "a string";
                case JTokenType.Boolean:
                    return "a boolean";
                case JTokenType.Integer:
                    return "a whole number";
                case JTokenType.Float:
                    return "a decimal number";
                case JTokenType.Object:
                    return "an object";
                case JTokenType.Array:
                    return "an array";
                case JTokenType.Null:
                    return "null";
                default:
                    return value.Type.ToString().ToLowerInvariant();
            }
        }

        static string PrepareConfigPath(string path, string defaultName, string extension)
        {
            var resolved = ProjectPaths.Resolve(string.IsNullOrWhiteSpace(path) ? defaultName : path, out var error);
            if (resolved == null)
                throw new ArgumentException(error);

            if (resolved.Equals(ProjectPaths.AuthoringRoot, StringComparison.OrdinalIgnoreCase))
                resolved += "/" + defaultName;

            if (resolved.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
                resolved = resolved.Substring(0, resolved.Length - extension.Length);
            resolved += extension;

            var folder = Path.GetDirectoryName(resolved)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(folder))
                CreateFolderRecursive(folder);

            return AssetDatabase.GenerateUniqueAssetPath(resolved);
        }

        static void CreateFolderRecursive(string assetsFolder)
        {
            if (AssetDatabase.IsValidFolder(assetsFolder))
                return;

            var parent = Path.GetDirectoryName(assetsFolder)?.Replace('\\', '/');
            var name = Path.GetFileName(assetsFolder);
            if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(name))
                throw new ArgumentException($"Invalid asset folder path '{assetsFolder}'.");

            if (!AssetDatabase.IsValidFolder(parent))
                CreateFolderRecursive(parent);

            AssetDatabase.CreateFolder(parent, name);
        }

        /// <summary>Result of creating a Remote Config deployment file.</summary>
        public class CreatedRemoteConfigDto
        {
            public string Type { get; set; }
            public string Path { get; set; }
            public List<string> Keys { get; set; }
            public Dictionary<string, string> Types { get; set; }
        }
    }
}
#endif
