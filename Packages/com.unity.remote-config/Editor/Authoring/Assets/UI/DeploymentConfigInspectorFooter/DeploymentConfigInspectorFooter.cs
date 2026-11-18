using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Unity.Services.DeploymentApi.Editor;
using Unity.Services.RemoteConfig.Authoring.Editor.Analytics;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Logger = Unity.Services.RemoteConfig.Editor.Shared.Logging.Logger;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace Unity.Services.RemoteConfig.Authoring.Editor.Assets.UI.DeploymentConfigInspectorFooter
{
#if UNITY_2023_3_OR_NEWER
    [UxmlElement]
#endif
    partial class DeploymentConfigInspectorFooter : BindableElement
    {
        string m_ServiceName;
        string m_FilePath;
        Func<Task<string>> m_DashboardUrlGetter;
        IConfigAnalytics m_ConfigAnalyticsSender;

        public Func<Task<string>> DashboardLinkUrlGetter
        {
            set
            {
                m_DashboardUrlGetter = value;
                SetupBtnViewInDashboard(this);
            }
        }

        public void BindGUI(string filePath, IConfigAnalytics analyticsSender, string serviceName = "")
        {
            SetupFooterVisual();
            SetupBtnViewInDeploymentWindow(this);
            SetupBtnViewInDashboard(this);
            m_ServiceName = serviceName ?? ReadPackageInfo().displayName;
            m_FilePath = filePath;
            m_ConfigAnalyticsSender = analyticsSender;
        }

        void SetupFooterVisual([CallerFilePath] string sourceFilePath = "")
        {
            var packageInfo = ReadPackageInfo();
            var packageName = packageInfo?.name ?? "com.unity.remote-config";
            var basePath = $"Packages/{packageName}/Editor/Authoring/Assets/UI/Assets";

            var uxmlPath = $"{basePath}/DeploymentConfigInspectorFooter.uxml";
            var ussPath = $"{basePath}/DeploymentConfigInspectorFooter.uss";
            var ussDarkPath = $"{basePath}/DeploymentConfigInspectorFooterDark.uss";
            var ussLightPath = $"{basePath}/DeploymentConfigInspectorFooterLight.uss";

            var mainStyleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(ussPath);
            if (mainStyleSheet != null)
                styleSheets.Add(mainStyleSheet);

            var themeStyleSheet = EditorGUIUtility.isProSkin 
                ? AssetDatabase.LoadAssetAtPath<StyleSheet>(ussDarkPath)
                : AssetDatabase.LoadAssetAtPath<StyleSheet>(ussLightPath);

            if (themeStyleSheet != null)
                styleSheets.Add(themeStyleSheet);

            var visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(uxmlPath);
            if (visualTreeAsset != null)
                visualTreeAsset.CloneTree(this);
        }

        static string RemovePathBeforePattern(string path, string pattern)
        {
            int patternIndex = path.LastIndexOf(pattern);
            if (patternIndex >= 0)
            {
                return path.Substring(patternIndex);
            }
            return path;
        }

        PackageInfo ReadPackageInfo()
        {
            return PackageInfo.FindForAssembly(GetType().Assembly);
        }

        void SetupBtnViewInDeploymentWindow(VisualElement myInspector)
        {
            var viewInDeployBtn = myInspector.Q<Button>("view-in-deployment-window-btn");
            if (viewInDeployBtn != null)
            {
                viewInDeployBtn.clickable.clicked += SelectFileInDeploymentWindow;
            }
        }

        void SetupBtnViewInDashboard(VisualElement myInspector)
        {
            var viewInDashboardBtn = myInspector.Q<Button>("view-in-dashboard-btn");
            if (viewInDashboardBtn != null)
            {
                var enabled = m_DashboardUrlGetter != null;
                viewInDashboardBtn.SetEnabled(enabled);

                if (enabled)
                {
                    viewInDashboardBtn.clicked -= OnViewInDashboardClicked;
                    viewInDashboardBtn.clicked += OnViewInDashboardClicked;
                }
            }
        }

        async void OnViewInDashboardClicked()
        {
            m_ConfigAnalyticsSender.SendViewInDashboardClickedEvent();
            var url = await m_DashboardUrlGetter();
            Application.OpenURL(url);
        }

        void SelectFileInDeploymentWindow()
        {
            if (File.Exists(m_FilePath))
            {
#if DEPLOYMENT_API_AVAILABLE_V1_1
                Deployments.Instance.DeploymentWindow.OpenWindow();
                Deployments.Instance.DeploymentWindow.ClearSelection();

                var deploymentItems = GetDeploymentItems();
                Deployments.Instance.DeploymentWindow.Select(deploymentItems);
                m_ConfigAnalyticsSender.SendViewInDeploymentWindowClickedEvent();
#elif DEPLOYMENT_API_AVAILABLE_V1_0
                Logger.Log("Please update your Deployment package to use this feature. A minimum version of 1.4.0 is required.");
#endif
            }
        }

#if DEPLOYMENT_API_AVAILABLE_V1_1
        List<IDeploymentItem> GetDeploymentItems()
        {
            var deploymentItems = Deployments.Instance.DeploymentWindow.GetFromFiles(new List<string> { m_FilePath });

            var simpleItems = deploymentItems.FindAll(x => x is not ICompositeItem);
            var compositeItems = deploymentItems.FindAll(x => x is ICompositeItem);

            // if any item is composite unwinds its children and add to the list
            if (compositeItems.Any())
            {
                foreach (var item in compositeItems)
                {
                    simpleItems.AddRange(((ICompositeItem)item).Children);
                }
            }

            return simpleItems;
        }

#endif

#if !UNITY_2023_3_OR_NEWER
        new class UxmlFactory : UxmlFactory<DeploymentConfigInspectorFooter> {}
#endif
    }
}
