#if UNITY_PIPELINE_AVAILABLE && UNITY_EDITOR && DEPLOYMENT_API_AVAILABLE_V1_0
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Unity.Pipeline.Commands;
using Unity.Pipeline.Editor.Authoring;
using Unity.Services.Core.Editor.Environments;
using Unity.Services.DeploymentApi.Editor;
using Unity.Services.RemoteConfig.Authoring.Editor.Deployment;
using Unity.Services.RemoteConfig.Authoring.Editor.Networking;
using Unity.Services.RemoteConfig.Editor.Authoring.Core.Deployment;
using Unity.Services.RemoteConfig.Editor.Authoring.Core.Model;
using Unity.Services.RemoteConfig.Editor.Authoring.Core.Service;
using UnityEditor;

namespace Unity.Services.RemoteConfig.Cli
{
    /// <summary>
    /// The Editor counterparts of `ugs remote-config export` and `ugs remote-config import`. They move an
    /// environment's whole config through the archive the UGS CLI uses, so an archive exported by either
    /// tool imports with the other. They reach the service through the package's own client, as the
    /// Deployment window does: the Editor's sign-in, the linked cloud project and the active environment.
    /// </summary>
    static class RemoteConfigExportImportEditorCliCommands
    {
        [CliGroupCommand(RemoteConfigCliConstants.Group,
            "Export the active Unity Services environment's Remote Config to a .rczip archive, like `ugs remote-config export`. "
            + "The archive imports with `services.remoteconfig import` or `ugs remote-config import`. Set the environment first with services.environment set_active_by_name.",
            Tags = new[] { RemoteConfigCliConstants.Service, RemoteConfigCliConstants.RemoteConfig })]
        public static Task<ExportResultDto> Export(
            [CliArg("out-dir", "Project folder to write the archive to (the Assets/ prefix is optional). Defaults to the authoring root, Assets.")] string outDir = null,
            [CliArg("file-name", "Archive file name, ending in .rczip. Defaults to ugs.rczip. An existing file is never overwritten.")] string fileName = null,
            [CliArg("dry-run", "List the keys that would be exported without writing the archive.")] bool dryRun = false)
        {
            var archivePath = ResolveExportPath(outDir, fileName);
            if (!dryRun && File.Exists(archivePath))
                throw new ArgumentException($"'{archivePath}' already exists. Export never overwrites: pass another file-name or out-dir.");

            return ExportAsync(archivePath, dryRun);
        }

        [CliGroupCommand(RemoteConfigCliConstants.Group,
            "Import a .rczip archive into the active Unity Services environment's Remote Config, like `ugs remote-config import`. "
            + "Archive keys are created or overwritten; the environment's other keys are kept, or deleted with reconcile. "
            + "This changes a live environment: run it with dry-run first, check EnvironmentName in the result, and pass environment to make the import fail unless that environment is the active one. "
            + "To deploy .rc files instead, use `services.deployment deploy_items`.",
            Tags = new[] { RemoteConfigCliConstants.Service, RemoteConfigCliConstants.RemoteConfig })]
        public static Task<ImportResultDto> Import(
            [CliArg("in-dir", "Folder holding the archive: a project folder (the Assets/ prefix is optional) or an absolute path. Defaults to the authoring root, Assets.")] string inDir = null,
            [CliArg("file-name", "Archive file name, ending in .rczip. Defaults to ugs.rczip.")] string fileName = null,
            [CliArg("dry-run", "List what would be created, updated and deleted without changing the environment.")] bool dryRun = false,
            [CliArg("reconcile", "Delete the environment's keys that are not in the archive. Without it they are kept.")] bool reconcile = false,
            [CliArg("environment", "Name of the environment you mean to import into. The import fails unless it is the active environment. Recommended.")] string environment = null,
            [CliArg("force", "With reconcile, allow an empty archive, which deletes every key in the environment.")] bool force = false)
        {
            var archivePath = ResolveImportPath(inDir, fileName);
            var local = RemoteConfigArchive.Read(archivePath);
            if (reconcile && local.Count == 0 && !dryRun && !force)
                throw new ArgumentException($"'{archivePath}' holds no keys, so importing it with reconcile would delete every key in the environment. "
                    + "Check the file, run with dry-run to see what would be deleted, or pass force to clear the environment.");

            return ImportAsync(archivePath, local, dryRun, reconcile, environment);
        }

        static async Task<ExportResultDto> ExportAsync(string archivePath, bool dryRun)
        {
            using var connection = Connection.ToActiveEnvironment();
            if (!dryRun)
                CreateFolderRecursive(Path.GetDirectoryName(archivePath)?.Replace('\\', '/'));

            var result = await new RemoteConfigEnvironmentExporter(connection.Client, connection.Target)
                .ExportAsync(archivePath, dryRun);
            result.EnvironmentName = connection.EnvironmentName;

            if (!dryRun && result.Path != null)
                AssetDatabase.ImportAsset(archivePath, ImportAssetOptions.ForceSynchronousImport);
            return result;
        }

        static async Task<ImportResultDto> ImportAsync(
            string archivePath, IReadOnlyList<RemoteConfigEntry> local, bool dryRun, bool reconcile, string expectedEnvironment)
        {
            using var connection = Connection.ToActiveEnvironment();
            CheckEnvironment(expectedEnvironment, connection.EnvironmentName);

            var result = await new RemoteConfigEnvironmentImporter(connection.Client, connection.Target)
                .ImportAsync(local, dryRun, reconcile);
            result.Path = archivePath;
            result.EnvironmentName = connection.EnvironmentName;
            return result;
        }

        /// <summary>Fails unless <paramref name="expected"/>, when given, names the active environment.</summary>
        internal static void CheckEnvironment(string expected, string active)
        {
            if (string.IsNullOrWhiteSpace(expected))
                return;
            if (!string.Equals(expected.Trim(), active, StringComparison.Ordinal))
                throw new InvalidOperationException($"environment is '{expected.Trim()}' but the active environment is '{active ?? "unknown"}'. Nothing was imported. "
                    + "Set the environment with services.environment set_active_by_name, or fix the environment argument.");
        }

        /// <summary>The archive to write: a project-relative path confined to the authoring root.</summary>
        internal static string ResolveExportPath(string outDir, string fileName)
        {
            var name = RemoteConfigArchive.ResolveFileName(fileName);
            return ResolveFolder(outDir) + "/" + name;
        }

        /// <summary>
        /// The archive to read. Resolved like <see cref="ResolveExportPath"/>, so an export and an import with
        /// the same arguments name the same file; an absolute path is also accepted, as reading writes nothing.
        /// </summary>
        internal static string ResolveImportPath(string inDir, string fileName)
        {
            var name = RemoteConfigArchive.ResolveFileName(fileName);
            if (!string.IsNullOrWhiteSpace(inDir) && Path.IsPathRooted(inDir.Trim()))
                return Path.Combine(inDir.Trim(), name).Replace('\\', '/');
            return ResolveFolder(inDir) + "/" + name;
        }

        static string ResolveFolder(string folder)
        {
            if (string.IsNullOrWhiteSpace(folder))
                return ProjectPaths.AuthoringRoot;

            var resolved = ProjectPaths.Resolve(folder, out var error);
            if (resolved == null)
                throw new ArgumentException(error);
            return resolved.TrimEnd('/');
        }

        /// <summary>
        /// The package's Remote Config client, targeting what the Deployment window targets: the linked
        /// cloud project and the active environment, authenticated with the Editor's sign-in.
        /// </summary>
        sealed class Connection : IDisposable
        {
            readonly RcWebApiClient m_WebApiClient;

            public IRemoteConfigClient Client { get; }
            public IDeploymentInfo Target { get; }
            public string EnvironmentName { get; }

            Connection(RcWebApiClient webApiClient, IDeploymentInfo target, string environmentName)
            {
                m_WebApiClient = webApiClient;
                Target = target;
                EnvironmentName = environmentName;
                Client = new RemoteConfigClient(webApiClient, target);
            }

            public static Connection ToActiveEnvironment()
            {
                if (Deployments.Instance.EnvironmentProvider == null)
                    throw new InvalidOperationException("No Unity Services environment provider is registered. Install com.unity.services.deployment, which supplies the active environment.");

                var target = new DeploymentInfo();
                if (string.IsNullOrEmpty(target.CloudProjectId))
                    throw new InvalidOperationException("This project is not linked to a Unity Cloud project. Link it in Project Settings > Services first.");
                if (string.IsNullOrEmpty(target.EnvironmentId))
                    throw new InvalidOperationException("No active Unity Services environment. Set one with services.environment set_active_by_name.");
                if (string.IsNullOrEmpty(CloudProjectSettings.accessToken))
                    throw new InvalidOperationException("The Editor is not signed in to Unity. Sign in, then retry.");

                return new Connection(new RcWebApiClient(), target, EnvironmentsApi.Instance?.ActiveEnvironmentName);
            }

            public void Dispose() => m_WebApiClient.Dispose();
        }

        /// <summary>A config key and its Remote Config type, as written in the archive.</summary>
        public class ConfigItemDto
        {
            public string Key { get; set; }
            public string Type { get; set; }
        }

        /// <summary>Result of services.remoteconfig export.</summary>
        public class ExportResultDto
        {
            public string ProjectId { get; set; }
            public string EnvironmentId { get; set; }
            public string EnvironmentName { get; set; }
            /// <summary>The archive written (or that would be, on a dry run); null when there was nothing to export.</summary>
            public string Path { get; set; }
            public bool DryRun { get; set; }
            public List<ConfigItemDto> Exported { get; set; }
            public string Message { get; set; }
        }

        /// <summary>Result of services.remoteconfig import.</summary>
        public class ImportResultDto
        {
            public string ProjectId { get; set; }
            public string EnvironmentId { get; set; }
            /// <summary>The environment imported into (or that would be, on a dry run).</summary>
            public string EnvironmentName { get; set; }
            public string Path { get; set; }
            public bool DryRun { get; set; }
            public bool Reconcile { get; set; }
            public List<string> Created { get; set; }
            public List<string> Updated { get; set; }
            /// <summary>Keys removed from the environment; only with reconcile.</summary>
            public List<string> Deleted { get; set; }
            /// <summary>Keys the archive does not hold that were left in place; only without reconcile.</summary>
            public List<string> Kept { get; set; }
        }

        internal static void CreateFolderRecursive(string assetsFolder)
        {
            if (AssetDatabase.IsValidFolder(assetsFolder))
                return;

            var parent = Path.GetDirectoryName(assetsFolder)?.Replace('\\', '/');
            var name = Path.GetFileName(assetsFolder);
            if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(name))
                throw new ArgumentException($"Invalid asset folder path '{assetsFolder}'.");

            if (!AssetDatabase.IsValidFolder(parent))
                CreateFolderRecursive(parent);

            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
#endif
