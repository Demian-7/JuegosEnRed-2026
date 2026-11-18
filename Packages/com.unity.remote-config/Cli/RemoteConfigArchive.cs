#if UNITY_PIPELINE_AVAILABLE && UNITY_EDITOR && DEPLOYMENT_API_AVAILABLE_V1_0
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Unity.Services.RemoteConfig.Editor.Authoring.Core.Model;

namespace Unity.Services.RemoteConfig.Cli
{
    /// <summary>
    /// The archive `ugs remote-config export` writes and `ugs remote-config import` reads: a zip
    /// (default name <c>ugs.rczip</c>) holding one entry, <c>rc-config</c>, whose content is a JSON array
    /// of <c>{"key", "value", "type"}</c> objects - the package's <see cref="RemoteConfigEntryDTO"/>, with
    /// the type in lower case. Archives are interchangeable between the UGS CLI and these commands.
    /// </summary>
    static class RemoteConfigArchive
    {
        public const string DefaultFileName = "ugs.rczip";
        public const string EntryName = "rc-config";

        static readonly string[] k_Types = { "string", "int", "bool", "float", "long", "json" };

        // DateParseHandling.None: a string value that looks like a date must stay a string, not become a
        // DateTime that Remote Config has no type for. Own settings, so JsonConvert.DefaultSettings set by
        // another package cannot change the format.
        static readonly JsonSerializerSettings k_Settings = new JsonSerializerSettings
        {
            DateParseHandling = DateParseHandling.None,
            FloatParseHandling = FloatParseHandling.Double,
        };

        /// <summary>
        /// The file name to use: <paramref name="fileName"/>, or <see cref="DefaultFileName"/> when it is
        /// omitted. As in the UGS CLI the extension must be <c>.rczip</c>; unlike it, the name must be a bare
        /// file name, since the directory is a separate argument and is what the path sandbox checks.
        /// </summary>
        public static string ResolveFileName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return DefaultFileName;

            var trimmed = fileName.Trim();
            if (trimmed.IndexOfAny(new[] { '/', '\\' }) >= 0 || trimmed == "." || trimmed == "..")
                throw new ArgumentException($"--file-name must be a file name without a directory, not '{fileName}'. Pass the directory separately.");

            var extension = Path.GetExtension(DefaultFileName);
            if (!string.Equals(Path.GetExtension(trimmed), extension, StringComparison.Ordinal))
                throw new ArgumentException($"--file-name must have the extension '{extension}'.");

            return trimmed;
        }

        /// <summary>The archive's entries as the UGS CLI writes them: key, lower-case type, value.</summary>
        public static List<RemoteConfigEntryDTO> ToDto(IEnumerable<RemoteConfigEntry> entries)
        {
            return entries.Select(entry => new RemoteConfigEntryDTO
            {
                key = entry.Key,
                type = entry.GetEntryConfigType().ToString().ToLowerInvariant(),
                value = entry.Value,
            }).ToList();
        }

        public static void Write(string archivePath, IEnumerable<RemoteConfigEntryDTO> entries)
        {
            var json = JsonConvert.SerializeObject(entries, k_Settings);
            using var file = new FileStream(archivePath, FileMode.CreateNew, FileAccess.Write);
            using var archive = new ZipArchive(file, ZipArchiveMode.Create);
            using var writer = new StreamWriter(archive.CreateEntry(EntryName).Open(), new UTF8Encoding(false));
            writer.Write(json);
        }

        /// <summary>
        /// Read an archive into config entries, rejecting what the service would reject or the UGS CLI
        /// would fail on halfway: a missing file, a malformed zip, empty or duplicate keys, unknown types and
        /// values that do not match their type.
        /// </summary>
        public static List<RemoteConfigEntry> Read(string archivePath)
        {
            if (!File.Exists(archivePath))
                throw new ArgumentException($"The file at '{archivePath}' could not be found. Ensure the file exists and the specified path is correct.");

            string json;
            try
            {
                using var file = new FileStream(archivePath, FileMode.Open, FileAccess.Read);
                using var archive = new ZipArchive(file, ZipArchiveMode.Read);
                var entry = archive.Entries.FirstOrDefault(e => e.FullName == EntryName)
                    ?? throw new ArgumentException($"The archive '{archivePath}' appears to be malformed: it has no '{EntryName}' entry.");
                using var reader = new StreamReader(entry.Open());
                json = reader.ReadToEnd();
            }
            catch (InvalidDataException exception)
            {
                throw new ArgumentException($"The archive '{archivePath}' appears to be malformed: {exception.Message}");
            }

            List<RemoteConfigEntryDTO> dtos;
            try
            {
                dtos = JsonConvert.DeserializeObject<List<RemoteConfigEntryDTO>>(json, k_Settings) ?? new List<RemoteConfigEntryDTO>();
            }
            catch (JsonException exception)
            {
                throw new ArgumentException($"The archive '{archivePath}' appears to be malformed: {exception.Message}");
            }

            return ToEntries(dtos);
        }

        internal static List<RemoteConfigEntry> ToEntries(IReadOnlyList<RemoteConfigEntryDTO> dtos)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var entries = new List<RemoteConfigEntry>(dtos.Count);
            foreach (var dto in dtos)
            {
                if (dto == null || string.IsNullOrWhiteSpace(dto.key))
                    throw new ArgumentException("The archive contains an entry without a key.");
                if (!seen.Add(dto.key))
                    throw new ArgumentException($"The archive contains the key '{dto.key}' more than once.");
                if (!k_Types.Contains(dto.type))
                    throw new ArgumentException($"Key '{dto.key}' has type '{dto.type}'. Valid types: {string.Join(", ", k_Types)}.");
                if (dto.value == null)
                    throw new ArgumentException($"Key '{dto.key}' has no value.");
                if (!Matches(dto.type, dto.value))
                    throw new ArgumentException($"Key '{dto.key}' is declared {dto.type} but its value '{dto.value}' is not.");

                entries.Add(dto.ToRemoteConfigEntry());
            }
            return entries;
        }

        // What RemoteConfigEntryDTO.ToValue can convert without throwing, and the service would accept.
        static bool Matches(string type, object value) => type switch
        {
            "string" => value is string,
            "bool" => value is bool,
            "int" => value is long number && number >= int.MinValue && number <= int.MaxValue,
            "long" => value is long,
            "float" => value is double or long,
            "json" => value is JObject or JArray,
            _ => false,
        };
    }
}
#endif
