using System.IO;
using Unity.Services.RemoteConfig.Editor.Authoring.Core.Model;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace Unity.Services.RemoteConfig.Authoring.Editor.Assets
{
    /// <summary>
    /// A generic custom importer for Remote Config files.
    /// Implement with custom assets and files to achieve custom inspectors or other behaviours.
    /// </summary>
    /// <typeparam name="TAsset">Must be of type RemoteConfigAsset</typeparam>
    /// <typeparam name="TFile">Must be of type RemoteConfigFile</typeparam>
    public class RemoteConfigImporter<TAsset, TFile> : ScriptedImporter
        where TAsset : RemoteConfigAsset
        where TFile : RemoteConfigFile, new()
    {
        const string k_RemoteConfigAssetIdentifier = "RemoteConfig";

        /// <inheritdoc/>
        public override void OnImportAsset(AssetImportContext ctx)
        {
            var remoteConfigAsset = ScriptableObject.CreateInstance<TAsset>();
            remoteConfigAsset.Model = new TFile()
            {
                Path = Path.Join(
                    Application.dataPath.Replace("Assets", ""),
                    ctx.assetPath)
            };

            ctx.AddObjectToAsset(k_RemoteConfigAssetIdentifier, remoteConfigAsset, RemoteConfigResources.Icon);
            ctx.SetMainObject(remoteConfigAsset);
        }

        /// <summary>
        /// This function is called when the importer is loaded or a value is changed in the Inspector.
        /// </summary>
        public void OnValidate()
        {
            hideFlags = HideFlags.HideInInspector;
        }
    }

    [RemoteConfigImporter]
    class RemoteConfigImporter : RemoteConfigImporter<RemoteConfigAsset, RemoteConfigFile>
    {
    }
}
