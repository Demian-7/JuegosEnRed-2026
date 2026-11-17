using System;
using System.Collections.Generic;
using Unity.Services.DeploymentApi.Editor;

namespace Unity.Services.RemoteConfig.Editor.Authoring.Core.Model
{
    /// <summary>
    /// Model representation of the Remote Config json file contents.
    /// </summary>
    [Serializable]
    public class RemoteConfigFile : DeploymentItem, IRemoteConfigFile
    {
        List<RemoteConfigEntry> m_Entries;

        /// <inheritdoc cref="IRemoteConfigFile.Path" />
        public sealed override string Path
        {
            get => base.Path;
            set
            {
                base.Path = value;
                Name = System.IO.Path.GetFileName(value);
            }
        }

        /// <inheritdoc cref="IRemoteConfigFile.Entries"/>
        public List<RemoteConfigEntry> Entries
        {
            get => m_Entries;
            set => SetField(ref m_Entries, value);
        }
    }
}
