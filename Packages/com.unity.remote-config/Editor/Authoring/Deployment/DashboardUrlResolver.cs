using System;
using System.Threading.Tasks;
using Unity.Services.Core.Editor.Environments;
using Unity.Services.Core.Editor.OrganizationHandler;
using Unity.Services.DeploymentApi.Editor;

namespace Unity.Services.RemoteConfig.Authoring.Editor.Deployment
{
    class DashboardUrlResolver : IDashboardUrlResolver
    {
        readonly IEnvironmentsApi m_EnvironmentsApi;
        readonly IProjectIdentifierProvider m_ProjectIdProvider;
        readonly IOrganizationHandler m_OrganizationHandler;

        public DashboardUrlResolver(
            IEnvironmentsApi environmentsApi,
            IProjectIdentifierProvider projectIdProvider,
            IOrganizationHandler organizationHandler)
        {
            m_EnvironmentsApi = environmentsApi;
            m_ProjectIdProvider = projectIdProvider;
            m_OrganizationHandler = organizationHandler;
        }

        public Task<string> RemoteConfig()
        {
            var projectId = m_ProjectIdProvider.ProjectId;
            var envId = m_EnvironmentsApi.ActiveEnvironmentId;
            var orgId = m_OrganizationHandler.Key;

            return Task.FromResult(
                $"https://cloud.unity.com/home/organizations/{orgId}/projects/{projectId}/environments/{envId}/remote-config/configs");
        }
    }
}
