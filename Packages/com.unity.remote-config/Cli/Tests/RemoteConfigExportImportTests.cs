#if UNITY_PIPELINE_AVAILABLE && DEPLOYMENT_API_AVAILABLE_V1_0
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Unity.Pipeline.Editor.Authoring;
using Unity.Services.RemoteConfig.Editor.Authoring.Core.Deployment;
using Unity.Services.RemoteConfig.Editor.Authoring.Core.Model;
using Unity.Services.RemoteConfig.Editor.Authoring.Core.Service;
using UnityEditor;

namespace Unity.Services.RemoteConfig.Cli.Tests
{
    class RemoteConfigExportImportTests
    {
        const string k_Folder = "Assets/CliRemoteConfigExportTests";

        string m_TempDirectory;
        string m_AuthoringRoot;
        FakeRemoteConfigClient m_Client;
        FakeTarget m_Target;

        [SetUp]
        public void SetUp()
        {
            m_TempDirectory = Path.Combine(Path.GetTempPath(), "rc-cli-tests-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(m_TempDirectory);
            m_AuthoringRoot = ProjectPaths.AuthoringRoot;
            ProjectPaths.AuthoringRoot = "Assets";
            m_Client = new FakeRemoteConfigClient();
            m_Target = new FakeTarget();
        }

        [TearDown]
        public void TearDown()
        {
            ProjectPaths.AuthoringRoot = m_AuthoringRoot;
            AssetDatabase.DeleteAsset(k_Folder);
            Directory.Delete(m_TempDirectory, true);
        }

        string TempArchive(string name = RemoteConfigArchive.DefaultFileName) => Path.Combine(m_TempDirectory, name);

        static List<RemoteConfigEntry> AllTypes() => new List<RemoteConfigEntry>
        {
            new RemoteConfigEntry { Key = "name", Value = "arena" },
            new RemoteConfigEntry { Key = "turn_timer", Value = 30 },
            new RemoteConfigEntry { Key = "enabled", Value = true },
            new RemoteConfigEntry { Key = "payout", Value = 1.5 },
            new RemoteConfigEntry { Key = "big", Value = 3000000000L },
            new RemoteConfigEntry { Key = "rewards", Value = JObject.Parse("{\"first_win\":{\"coins\":10}}") },
            new RemoteConfigEntry { Key = "levels", Value = JArray.Parse("[1,2,3]") },
        };

        static T Run<T>(Task<T> task) => task.GetAwaiter().GetResult();

        static void WriteRawArchive(string path, string entryName, string json)
        {
            using var file = new FileStream(path, FileMode.CreateNew);
            using var archive = new ZipArchive(file, ZipArchiveMode.Create);
            using var writer = new StreamWriter(archive.CreateEntry(entryName).Open(), new UTF8Encoding(false));
            writer.Write(json);
        }

        static string ReadRawEntry(string path)
        {
            using var file = new FileStream(path, FileMode.Open, FileAccess.Read);
            using var archive = new ZipArchive(file, ZipArchiveMode.Read);
            Assert.That(archive.Entries.Select(e => e.FullName), Is.EqualTo(new[] { "rc-config" }));
            using var reader = new StreamReader(archive.Entries[0].Open());
            return reader.ReadToEnd();
        }

        // ---- Archive format ----

        [Test]
        public void ResolveFileName_Omitted_IsTheUgsDefault()
        {
            Assert.That(RemoteConfigArchive.ResolveFileName(null), Is.EqualTo("ugs.rczip"));
            Assert.That(RemoteConfigArchive.ResolveFileName(" "), Is.EqualTo("ugs.rczip"));
            Assert.That(RemoteConfigArchive.ResolveFileName("staging.rczip"), Is.EqualTo("staging.rczip"));
        }

        [TestCase("staging.zip")]
        [TestCase("staging")]
        [TestCase("../staging.rczip")]
        [TestCase("sub/staging.rczip")]
        [TestCase("sub\\staging.rczip")]
        public void ResolveFileName_WrongExtensionOrDirectory_IsRejected(string fileName)
        {
            Assert.Throws<ArgumentException>(() => RemoteConfigArchive.ResolveFileName(fileName));
        }

        [Test]
        public void Write_UsesTheUgsCliLayout()
        {
            var path = TempArchive();
            RemoteConfigArchive.Write(path, RemoteConfigArchive.ToDto(AllTypes()));

            var items = JArray.Parse(ReadRawEntry(path));
            Assert.That(items.Select(item => item["key"].Value<string>()),
                Is.EqualTo(new[] { "name", "turn_timer", "enabled", "payout", "big", "rewards", "levels" }));
            Assert.That(items.Select(item => item["type"].Value<string>()),
                Is.EqualTo(new[] { "string", "int", "bool", "float", "long", "json", "json" }));
            Assert.That(items[5]["value"]["first_win"]["coins"].Value<int>(), Is.EqualTo(10));
        }

        [Test]
        public void WriteThenRead_RoundTripsEveryType()
        {
            var path = TempArchive();
            var original = AllTypes();
            RemoteConfigArchive.Write(path, RemoteConfigArchive.ToDto(original));

            var read = RemoteConfigArchive.Read(path);

            Assert.That(read.Select(e => e.Key), Is.EqualTo(original.Select(e => e.Key)));
            Assert.That(read.Select(e => e.GetEntryConfigType()), Is.EqualTo(original.Select(e => e.GetEntryConfigType())));
            Assert.That(read[1].Value, Is.EqualTo(30).And.TypeOf<int>());
            Assert.That(read[3].Value, Is.EqualTo(1.5).And.TypeOf<double>());
            Assert.That(read[4].Value, Is.EqualTo(3000000000L).And.TypeOf<long>());
            Assert.That(JToken.DeepEquals((JToken)read[5].Value, (JToken)original[5].Value), Is.True);
            Assert.That(JToken.DeepEquals((JToken)read[6].Value, (JToken)original[6].Value), Is.True);
        }

        [Test]
        public void Read_ArchiveAsTheUgsCliWritesIt_IsParsed()
        {
            var path = TempArchive();
            WriteRawArchive(path, "rc-config",
                "[{\"key\":\"when\",\"value\":\"2024-01-01T00:00:00Z\",\"type\":\"string\"},"
                + "{\"key\":\"ratio\",\"value\":2,\"type\":\"float\"},"
                + "{\"key\":\"lives\",\"value\":3,\"type\":\"int\"}]");

            var read = RemoteConfigArchive.Read(path);

            Assert.That(read[0].Value, Is.EqualTo("2024-01-01T00:00:00Z").And.TypeOf<string>());
            Assert.That(read[1].Value, Is.EqualTo(2.0).And.TypeOf<double>());
            Assert.That(read[2].Value, Is.EqualTo(3).And.TypeOf<int>());
        }

        [Test]
        public void Read_MissingFile_IsRejected()
        {
            Assert.Throws<ArgumentException>(() => RemoteConfigArchive.Read(TempArchive("absent.rczip")));
        }

        [Test]
        public void Read_NotAZip_IsRejected()
        {
            var path = TempArchive();
            File.WriteAllText(path, "not a zip");
            Assert.Throws<ArgumentException>(() => RemoteConfigArchive.Read(path));
        }

        [Test]
        public void Read_ZipWithoutTheConfigEntry_IsRejected()
        {
            var path = TempArchive();
            WriteRawArchive(path, "cc-scripts", "[]");
            Assert.Throws<ArgumentException>(() => RemoteConfigArchive.Read(path));
        }

        [TestCase("not json")]
        [TestCase("{\"key\":\"a\"}")]
        [TestCase("[{\"value\":1,\"type\":\"int\"}]")]
        [TestCase("[{\"key\":\"a\",\"value\":1,\"type\":\"int\"},{\"key\":\"a\",\"value\":2,\"type\":\"int\"}]")]
        [TestCase("[{\"key\":\"a\",\"value\":1,\"type\":\"double\"}]")]
        [TestCase("[{\"key\":\"a\",\"value\":null,\"type\":\"string\"}]")]
        [TestCase("[{\"key\":\"a\",\"value\":\"1\",\"type\":\"int\"}]")]
        [TestCase("[{\"key\":\"a\",\"value\":1.5,\"type\":\"int\"}]")]
        [TestCase("[{\"key\":\"a\",\"value\":3000000000,\"type\":\"int\"}]")]
        [TestCase("[{\"key\":\"a\",\"value\":\"{}\",\"type\":\"json\"}]")]
        public void Read_InvalidContent_IsRejected(string json)
        {
            var path = TempArchive();
            WriteRawArchive(path, "rc-config", json);
            Assert.Throws<ArgumentException>(() => RemoteConfigArchive.Read(path));
        }

        // ---- Export ----

        [Test]
        public void Export_WritesTheEnvironmentConfig()
        {
            m_Client.SetRemote(AllTypes());
            var path = TempArchive();

            var result = Run(new RemoteConfigEnvironmentExporter(m_Client, m_Target).ExportAsync(path, dryRun: false));

            Assert.That(result.Path, Is.EqualTo(path));
            Assert.That(result.ProjectId, Is.EqualTo(FakeTarget.ProjectId));
            Assert.That(result.EnvironmentId, Is.EqualTo(FakeTarget.EnvironmentIdValue));
            Assert.That(result.DryRun, Is.False);
            Assert.That(result.Exported.Select(item => item.Key), Is.EqualTo(AllTypes().Select(e => e.Key)));
            Assert.That(result.Exported[4].Type, Is.EqualTo("long"));
            Assert.That(RemoteConfigArchive.Read(path).Count, Is.EqualTo(7));
            Assert.That(m_Client.Writes, Is.EqualTo(0));
        }

        [Test]
        public void Export_DryRun_ListsWithoutWriting()
        {
            m_Client.SetRemote(AllTypes());
            var path = TempArchive();

            var result = Run(new RemoteConfigEnvironmentExporter(m_Client, m_Target).ExportAsync(path, dryRun: true));

            Assert.That(result.DryRun, Is.True);
            Assert.That(result.Exported, Has.Count.EqualTo(7));
            Assert.That(File.Exists(path), Is.False);
        }

        [Test]
        public void Export_EnvironmentWithoutConfig_WritesNothing()
        {
            var path = TempArchive();

            var result = Run(new RemoteConfigEnvironmentExporter(m_Client, m_Target).ExportAsync(path, dryRun: false));

            Assert.That(result.Path, Is.Null);
            Assert.That(result.Exported, Is.Empty);
            Assert.That(result.Message, Is.EqualTo(RemoteConfigEnvironmentExporter.NoContentMessage));
            Assert.That(File.Exists(path), Is.False);
        }

        // ---- Import ----

        static List<RemoteConfigEntry> Archive() => new List<RemoteConfigEntry>
        {
            new RemoteConfigEntry { Key = "shared", Value = "from archive" },
            new RemoteConfigEntry { Key = "archive_only", Value = 7 },
        };

        static List<RemoteConfigEntry> Environment() => new List<RemoteConfigEntry>
        {
            new RemoteConfigEntry { Key = "shared", Value = "from environment" },
            new RemoteConfigEntry { Key = "environment_only", Value = true },
        };

        [Test]
        public void Import_EnvironmentWithoutConfig_CreatesIt()
        {
            var result = Run(new RemoteConfigEnvironmentImporter(m_Client, m_Target).ImportAsync(Archive(), dryRun: false, reconcile: false));

            Assert.That(m_Client.Created.Select(e => e.Key), Is.EqualTo(new[] { "shared", "archive_only" }));
            Assert.That(m_Client.Updated, Is.Null);
            Assert.That(result.Created, Is.EqualTo(new[] { "shared", "archive_only" }));
            Assert.That(result.Updated, Is.Empty);
            Assert.That(result.Deleted, Is.Empty);
            Assert.That(result.Kept, Is.Empty);
        }

        [Test]
        public void Import_WithoutReconcile_OverwritesArchiveKeysAndKeepsTheRest()
        {
            m_Client.SetRemote(Environment());

            var result = Run(new RemoteConfigEnvironmentImporter(m_Client, m_Target).ImportAsync(Archive(), dryRun: false, reconcile: false));

            Assert.That(m_Client.Created, Is.Null);
            Assert.That(m_Client.Updated.ToDictionary(e => e.Key, e => e.Value), Is.EquivalentTo(new Dictionary<string, object>
            {
                ["shared"] = "from archive",
                ["archive_only"] = 7,
                ["environment_only"] = true,
            }));
            Assert.That(result.Created, Is.EqualTo(new[] { "archive_only" }));
            Assert.That(result.Updated, Is.EqualTo(new[] { "shared" }));
            Assert.That(result.Deleted, Is.Empty);
            Assert.That(result.Kept, Is.EqualTo(new[] { "environment_only" }));
            Assert.That(result.ProjectId, Is.EqualTo(FakeTarget.ProjectId));
            Assert.That(result.EnvironmentId, Is.EqualTo(FakeTarget.EnvironmentIdValue));
        }

        [Test]
        public void Import_WithReconcile_DeletesKeysNotInTheArchive()
        {
            m_Client.SetRemote(Environment());

            var result = Run(new RemoteConfigEnvironmentImporter(m_Client, m_Target).ImportAsync(Archive(), dryRun: false, reconcile: true));

            Assert.That(m_Client.Updated.Select(e => e.Key), Is.EquivalentTo(new[] { "shared", "archive_only" }));
            Assert.That(result.Deleted, Is.EqualTo(new[] { "environment_only" }));
            Assert.That(result.Kept, Is.Empty);
            Assert.That(result.Reconcile, Is.True);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Import_DryRun_ReportsWithoutWriting(bool reconcile)
        {
            m_Client.SetRemote(Environment());

            var result = Run(new RemoteConfigEnvironmentImporter(m_Client, m_Target).ImportAsync(Archive(), dryRun: true, reconcile: reconcile));

            Assert.That(m_Client.Writes, Is.EqualTo(0));
            Assert.That(result.DryRun, Is.True);
            Assert.That(result.Created, Is.EqualTo(new[] { "archive_only" }));
            Assert.That(result.Updated, Is.EqualTo(new[] { "shared" }));
            Assert.That(reconcile ? result.Deleted : result.Kept, Is.EqualTo(new[] { "environment_only" }));
        }

        [Test]
        public void ExportThenImport_CopiesAnEnvironmentWithItsTypes()
        {
            var source = new FakeRemoteConfigClient();
            source.SetRemote(AllTypes());
            var path = TempArchive();
            Run(new RemoteConfigEnvironmentExporter(source, m_Target).ExportAsync(path, dryRun: false));

            Run(new RemoteConfigEnvironmentImporter(m_Client, m_Target).ImportAsync(RemoteConfigArchive.Read(path), dryRun: false, reconcile: false));

            Assert.That(m_Client.Created.Select(e => (e.Key, e.GetEntryConfigType())),
                Is.EqualTo(AllTypes().Select(e => (e.Key, e.GetEntryConfigType()))));
        }

        // ---- Command arguments (validated before anything goes online) ----

        [Test]
        public void ResolveExportPath_Defaults_ToTheAuthoringRoot()
        {
            Assert.That(RemoteConfigExportImportEditorCliCommands.ResolveExportPath(null, null), Is.EqualTo("Assets/ugs.rczip"));
            Assert.That(RemoteConfigExportImportEditorCliCommands.ResolveExportPath("Exports", "prod.rczip"), Is.EqualTo("Assets/Exports/prod.rczip"));
            Assert.That(RemoteConfigExportImportEditorCliCommands.ResolveExportPath("Assets/Exports/", null), Is.EqualTo("Assets/Exports/ugs.rczip"));
        }

        [Test]
        public void ResolveImportPath_MatchesExportAndAcceptsAbsolutePaths()
        {
            Assert.That(RemoteConfigExportImportEditorCliCommands.ResolveImportPath("Exports", "prod.rczip"),
                Is.EqualTo(RemoteConfigExportImportEditorCliCommands.ResolveExportPath("Exports", "prod.rczip")));
            Assert.That(RemoteConfigExportImportEditorCliCommands.ResolveImportPath(m_TempDirectory, null),
                Is.EqualTo(Path.Combine(m_TempDirectory, "ugs.rczip").Replace('\\', '/')));
        }

        [Test]
        public void Export_OutDirOutsideTheProject_IsRejected()
        {
            Assert.Throws<ArgumentException>(() => RemoteConfigExportImportEditorCliCommands.Export("../outside"));
            Assert.Throws<ArgumentException>(() => RemoteConfigExportImportEditorCliCommands.Export(m_TempDirectory));
        }

        [Test]
        public void Export_WrongFileExtension_IsRejected()
        {
            Assert.Throws<ArgumentException>(() => RemoteConfigExportImportEditorCliCommands.Export(k_Folder, "config.zip"));
        }

        [Test]
        public void Export_ExistingArchive_IsNeverOverwritten()
        {
            RemoteConfigExportImportEditorCliCommands.CreateFolderRecursive(k_Folder);
            File.WriteAllText(k_Folder + "/ugs.rczip", "keep me");

            Assert.Throws<ArgumentException>(() => RemoteConfigExportImportEditorCliCommands.Export(k_Folder));
            Assert.That(File.ReadAllText(k_Folder + "/ugs.rczip"), Is.EqualTo("keep me"));
        }

        [Test]
        public void Import_MissingArchive_IsRejected()
        {
            Assert.Throws<ArgumentException>(() => RemoteConfigExportImportEditorCliCommands.Import(m_TempDirectory));
        }

        [Test]
        public void Import_InvalidArchive_IsRejected()
        {
            WriteRawArchive(TempArchive(), "rc-config", "[{\"key\":\"a\",\"value\":1,\"type\":\"double\"}]");
            Assert.Throws<ArgumentException>(() => RemoteConfigExportImportEditorCliCommands.Import(m_TempDirectory, dryRun: true));
        }

        [Test]
        public void Import_EmptyArchiveWithReconcile_IsRefusedUnlessForced()
        {
            WriteRawArchive(TempArchive(), "rc-config", "[]");

            var exception = Assert.Throws<ArgumentException>(() => RemoteConfigExportImportEditorCliCommands.Import(m_TempDirectory, reconcile: true));
            Assert.That(exception.Message, Does.Contain("delete every key"));
        }

        [TestCase(null, "production")]
        [TestCase("", "production")]
        [TestCase("staging", "staging")]
        [TestCase(" staging ", "staging")]
        public void CheckEnvironment_OmittedOrMatching_Passes(string expected, string active)
        {
            Assert.DoesNotThrow(() => RemoteConfigExportImportEditorCliCommands.CheckEnvironment(expected, active));
        }

        [TestCase("staging", "production")]
        [TestCase("Staging", "staging")]
        [TestCase("staging", null)]
        public void CheckEnvironment_Mismatch_IsRejected(string expected, string active)
        {
            Assert.Throws<InvalidOperationException>(() => RemoteConfigExportImportEditorCliCommands.CheckEnvironment(expected, active));
        }

        class FakeTarget : IDeploymentInfo
        {
            public const string ProjectId = "project-id";
            public const string EnvironmentIdValue = "environment-id";

            public string EnvironmentId => EnvironmentIdValue;
            public string CloudProjectId => ProjectId;
        }

        class FakeRemoteConfigClient : IRemoteConfigClient
        {
            GetConfigsResult m_Remote = new GetConfigsResult(false, null);

            public IReadOnlyList<RemoteConfigEntry> Created { get; private set; }
            public IReadOnlyList<RemoteConfigEntry> Updated { get; private set; }
            public int Writes { get; private set; }

            public void SetRemote(IReadOnlyList<RemoteConfigEntry> entries) => m_Remote = new GetConfigsResult(true, entries);

            public Task<GetConfigsResult> GetAsync() => Task.FromResult(m_Remote);

            public Task CreateAsync(IReadOnlyList<RemoteConfigEntry> remoteConfigEntries)
            {
                Writes++;
                Created = remoteConfigEntries;
                return Task.CompletedTask;
            }

            public Task UpdateAsync(IReadOnlyList<RemoteConfigEntry> remoteConfigEntries)
            {
                Writes++;
                Updated = remoteConfigEntries;
                return Task.CompletedTask;
            }
        }
    }
}
#endif
