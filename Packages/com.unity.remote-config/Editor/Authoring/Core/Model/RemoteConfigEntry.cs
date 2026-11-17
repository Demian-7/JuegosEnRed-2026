using System;
using Newtonsoft.Json.Linq;
using Unity.Services.RemoteConfig.Editor.Authoring.Core.Formatting;

namespace Unity.Services.RemoteConfig.Editor.Authoring.Core.Model
{
    /// <summary>
    /// The Key-Value combination for an entry in a Remote Config file.
    /// </summary>
    public class RemoteConfigEntry
    {
        /// <summary>
        /// Key of the entry
        /// </summary>
        public string Key;
        /// <summary>
        /// Value of the entry
        /// </summary>
        public object Value;
        /// <summary>
        /// Nested file (if it exists)
        /// </summary>
        public IRemoteConfigFile File;
        
        /// <summary>
        /// Retrieves the ConfigType for the entry based on its Value
        /// </summary>
        /// <returns>ConfigType for the entry</returns>
        /// <exception cref="ArgumentNullException">If the argument is not one of the supported types</exception>
        public ConfigType GetEntryConfigType()
        {
            switch (Value)
            {
                case string:
                    return ConfigType.STRING;
                case int:
                    return ConfigType.INT;
                case bool:
                    return ConfigType.BOOL;
                case float:
                case double:
                    return ConfigType.FLOAT;
                case long:
                    return ConfigType.LONG;
                case JArray:
                case JObject:
                    return ConfigType.JSON;
                default:
                    throw new ArgumentNullException(nameof(Value));
            }
        }
    }
}
