#if UNITY_PIPELINE_AVAILABLE && UNITY_EDITOR && DEPLOYMENT_API_AVAILABLE_V1_0
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Services.RemoteConfig.Editor.Authoring.Core.Deployment;
using Unity.Services.RemoteConfig.Editor.Authoring.Core.Model;
using Unity.Services.RemoteConfig.Editor.Authoring.Core.Service;

namespace Unity.Services.RemoteConfig.Cli
{
    /// <summary>
    /// `ugs remote-config export` over the package's own Remote Config client: read the environment's
    /// settings config and write it to a UGS export archive.
    /// </summary>
    sealed class RemoteConfigEnvironmentExporter
    {
        internal const string NoContentMessage = "No content to export.";

        readonly IRemoteConfigClient m_Client;
        readonly IDeploymentInfo m_Target;

        public RemoteConfigEnvironmentExporter(IRemoteConfigClient client, IDeploymentInfo target)
        {
            m_Client = client;
            m_Target = target;
        }

        /// <summary>
        /// Fetch the environment's config and, unless <paramref name="dryRun"/>, write it to
        /// <paramref name="archivePath"/>. As in the UGS CLI, an environment without config writes no file.
        /// </summary>
        public async Task<RemoteConfigExportImportEditorCliCommands.ExportResultDto> ExportAsync(string archivePath, bool dryRun)
        {
            var remote = await m_Client.GetAsync();
            var entries = remote.ConfigsExists && remote.Configs != null
                ? remote.Configs
                : Array.Empty<RemoteConfigEntry>();
            var dtos = RemoteConfigArchive.ToDto(entries);

            var result = new RemoteConfigExportImportEditorCliCommands.ExportResultDto
            {
                ProjectId = m_Target.CloudProjectId,
                EnvironmentId = m_Target.EnvironmentId,
                DryRun = dryRun,
                Exported = dtos
                    .Select(dto => new RemoteConfigExportImportEditorCliCommands.ConfigItemDto { Key = dto.key, Type = dto.type })
                    .ToList(),
            };

            if (dtos.Count == 0)
            {
                result.Message = NoContentMessage;
                return result;
            }

            if (!dryRun)
                RemoteConfigArchive.Write(archivePath, dtos);

            result.Path = archivePath;
            return result;
        }
    }
}
#endif
