using System;
using System.IO;
using System.Linq;
using Unity.Services.RemoteConfig.Authoring.Editor.Analytics;
using Unity.Services.RemoteConfig.Authoring.Editor.Deployment;
using UnityEditor;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Unity.Services.RemoteConfig.Authoring.Editor.Assets.UI
{
    [CustomEditor(typeof(RemoteConfigAsset))]
    [CanEditMultipleObjects]
    class CloudCodeScriptInspector : UnityEditor.Editor
    {
        const int k_MaxLines = 75;
        const string k_Template = "Packages/com.unity.remote-config/Editor/Authoring/Assets/UI/Assets/RemoteConfigInspector.uxml";

        public override VisualElement CreateInspectorGUI()
        {
            var myInspector = new VisualElement();
            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(k_Template);
            visualTree.CloneTree(myInspector);

            ShowScriptBody(myInspector);
            SetupConfigFooter(myInspector);

            return myInspector;
        }

        void SetupConfigFooter(VisualElement rootElement)
        {
            var deploymentConfigInspectorFooter = rootElement.Q<DeploymentConfigInspectorFooter.DeploymentConfigInspectorFooter>();
            var assetPath = AssetDatabase.GetAssetPath(target);
            deploymentConfigInspectorFooter.BindGUI(
                assetPath,
                RemoteConfigServices.Instance.GetService<IConfigAnalytics>(),
                "remote-config");
            deploymentConfigInspectorFooter.DashboardLinkUrlGetter = () => RemoteConfigServices.Instance
                .GetService<IDashboardUrlResolver>()
                .RemoteConfig();
        }

        void ShowScriptBody(VisualElement myInspector)
        {
            var body = myInspector.Q<TextField>();
            if (targets.Length == 1)
            {
                body.visible = true;
                body.value = ReadScriptBody(targets[0]);
            }
            else
            {
                body.visible = false;
            }
        }

        static string ReadScriptBody(Object script)
        {
            var path = AssetDatabase.GetAssetPath(script);
            var lines = File.ReadLines(path).Take(k_MaxLines).ToList();
            if (lines.Count == k_MaxLines)
            {
                lines.Add("...");
            }
            return string.Join(Environment.NewLine, lines);
        }
    }
}
