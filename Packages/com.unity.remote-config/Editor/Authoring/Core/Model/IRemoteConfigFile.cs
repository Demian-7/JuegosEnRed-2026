using System.Collections.Generic;

namespace Unity.Services.RemoteConfig.Editor.Authoring.Core.Model
{
    /// <summary>
    /// Interface for Remote Config files
    /// </summary>
    public interface IRemoteConfigFile
    {
        /// <summary>
        /// Name of the file
        /// </summary>
        string Name { get; }
        
        /// <summary>
        /// Path of the file
        /// </summary>
        string Path { get; set; }
        
        /// <summary>
        /// List of the contents of the file
        /// </summary>
        List<RemoteConfigEntry> Entries { get; set; }
    }
}
