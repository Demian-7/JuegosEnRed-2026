using Unity.Services.RemoteConfig.Editor.Authoring.Core.Model;
using Unity.Services.RemoteConfig.Editor.Shared.Assets;
using Unity.Services.RemoteConfig.Editor.Shared.EditorUtils;
using UnityEngine;

namespace Unity.Services.RemoteConfig.Authoring.Editor.Assets
{
    /// <summary>
    /// Scriptable Object used for Configuration as Code in the Project
    /// </summary>
    public class RemoteConfigAsset : ScriptableObject, IPath, ISerializationCallbackReceiver
    {
        /// <summary>
        /// The model representing the contents of the file
        /// </summary>
        protected RemoteConfigFile m_Model;
        
        /// <summary>
        /// Default ctor for RemoteConfigAsset
        /// </summary>
        public RemoteConfigAsset()
        {
            Model = new RemoteConfigFile();
        }
        
        /// <summary>
        /// The model representing the contents of the file
        /// </summary>
        public RemoteConfigFile Model
        {
            get => m_Model;
            internal set => m_Model = value;
        }
        
        /// <summary>
        /// The path of the asset
        /// </summary>
        public string Path
        {
            get => m_Model.Path;
            set => m_Model.Path = value;
        }

        /// <inheritdoc/>
        public void OnBeforeSerialize()
        {
        }

        /// <inheritdoc/>
        public void OnAfterDeserialize()
        {
            Sync.RunNextUpdateOnMain(() =>
            {
                if (m_Model.Path != null)
                {
                    m_Model.Name = System.IO.Path.GetFileName(Path);
                }
            });
        }
    }
}
