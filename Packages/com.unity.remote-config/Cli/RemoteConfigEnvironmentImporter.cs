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
    /// `ugs remote-config import` over the package's own Remote Config client. An environment holds a
    /// single settings config, so - like the UGS CLI - the import is one write of the whole key set rather
    /// than per-key calls: the archive's keys, plus the environment's other keys unless reconciling.
    /// </summary>
    sealed class RemoteConfigEnvironmentImporter
    {
        readonly IRemoteConfigClient m_Client;
        readonly IDeploymentInfo m_Target;

        public RemoteConfigEnvironmentImporter(IRemoteConfigClient client, IDeploymentInfo target)
        {
            m_Client = client;
            m_Target = target;
        }

        /// <summary>
        /// Compare <paramref name="local"/> with the environment and, unless <paramref name="dryRun"/>, write
        /// the result. Keys only in the archive are created and keys on both sides are updated to the
        /// archive's value. Keys only in the environment are deleted when <paramref name="reconcile"/>, and
        /// kept otherwise.
        /// </summary>
        public async Task<RemoteConfigExportImportEditorCliCommands.ImportResultDto> ImportAsync(
            IReadOnlyList<RemoteConfigEntry> local, bool dryRun, bool reconcile)
        {
            // GetAsync also records the environment's config id, which UpdateAsync needs.
            var remote = await m_Client.GetAsync();
            var remoteEntries = remote.ConfigsExists && remote.Configs != null
                ? remote.Configs
                : Array.Empty<RemoteConfigEntry>();

            var remoteKeys = new HashSet<string>(remoteEntries.Select(entry => entry.Key), StringComparer.Ordinal);
            var localKeys = new HashSet<string>(local.Select(entry => entry.Key), StringComparer.Ordinal);
            var remoteOnly = remoteEntries.Where(entry => !localKeys.Contains(entry.Key)).ToList();

            var payload = reconcile ? local.ToList() : local.Concat(remoteOnly).ToList();

            if (!dryRun)
            {
                if (remote.ConfigsExists)
                    await m_Client.UpdateAsync(payload);
                else if (payload.Count > 0)
                    await m_Client.CreateAsync(payload);
            }

            var remoteOnlyKeys = remoteOnly.Select(entry => entry.Key).ToList();
            return new RemoteConfigExportImportEditorCliCommands.ImportResultDto
            {
                ProjectId = m_Target.CloudProjectId,
                EnvironmentId = m_Target.EnvironmentId,
                DryRun = dryRun,
                Reconcile = reconcile,
                Created = local.Where(entry => !remoteKeys.Contains(entry.Key)).Select(entry => entry.Key).ToList(),
                Updated = local.Where(entry => remoteKeys.Contains(entry.Key)).Select(entry => entry.Key).ToList(),
                Deleted = reconcile ? remoteOnlyKeys : new List<string>(),
                Kept = reconcile ? new List<string>() : remoteOnlyKeys,
            };
        }
    }
}
#endif
