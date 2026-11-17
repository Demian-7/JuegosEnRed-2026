using System;
using System.Threading.Tasks;

namespace Unity.Services.RemoteConfig.Authoring.Editor.Deployment
{
    interface IDashboardUrlResolver
    {
        Task<string> RemoteConfig();
    }
}
