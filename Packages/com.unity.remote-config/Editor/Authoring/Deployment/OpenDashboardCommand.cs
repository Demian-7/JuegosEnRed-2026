using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Unity.Services.DeploymentApi.Editor;
using UnityEditor;
using UnityEngine;

namespace Unity.Services.RemoteConfig.Authoring.Editor.Deployment
{
    class OpenDashboardCommand : Command
    {
        readonly IDashboardUrlResolver m_DashboardUrlResolver;
        public override string Name => L10n.Tr("Open in Dashboard");

        public OpenDashboardCommand(IDashboardUrlResolver dashboardUrlResolver)
        {
            m_DashboardUrlResolver = dashboardUrlResolver;
        }

        public override async Task ExecuteAsync(IEnumerable<IDeploymentItem> items, CancellationToken cancellationToken = default)
        {
            Application.OpenURL(await m_DashboardUrlResolver.RemoteConfig());
        }
    }
}
