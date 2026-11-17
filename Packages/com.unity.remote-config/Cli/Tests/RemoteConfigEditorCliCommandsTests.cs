#if UNITY_PIPELINE_AVAILABLE && DEPLOYMENT_API_AVAILABLE_V1_0
using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Unity.Pipeline.Editor.Authoring;
using UnityEditor;

namespace Unity.Services.RemoteConfig.Cli.Tests
{
    class RemoteConfigEditorCliCommandsTests
    {
        const string k_Folder = "Assets/CliRemoteConfigTests";

        readonly List<string> m_Created = new List<string>();
        string m_AuthoringRoot;

        [SetUp]
        public void SetUp()
        {
            m_AuthoringRoot = ProjectPaths.AuthoringRoot;
            ProjectPaths.AuthoringRoot = "Assets";
        }

        [TearDown]
        public void TearDown()
        {
            ProjectPaths.AuthoringRoot = m_AuthoringRoot;
            foreach (var path in m_Created)
                AssetDatabase.DeleteAsset(path);
            m_Created.Clear();
            AssetDatabase.DeleteAsset(k_Folder);
        }

        RemoteConfigEditorCliCommands.CreatedRemoteConfigDto Create(string path = null, string entries = null, string types = null)
        {
            var dto = RemoteConfigEditorCliCommands.CreateConfig(path, entries, types);
            m_Created.Add(dto.Path);
            return dto;
        }

        static JObject Read(string assetPath) => JObject.Parse(File.ReadAllText(assetPath));

        [Test]
        public void CreateConfig_NoArguments_CreatesEmptyDefaultFile()
        {
            var dto = Create();

            Assert.That(dto.Type, Is.EqualTo("RemoteConfigFile"));
            Assert.That(dto.Path, Does.StartWith("Assets/remote_config").And.EndWith(".rc"));
            Assert.That(dto.Keys, Is.Empty);
            Assert.That(dto.Types, Is.Empty);
            var document = Read(dto.Path);
            Assert.That(((JObject)document["entries"]).Count, Is.EqualTo(0));
            Assert.That(((JObject)document["types"]).Count, Is.EqualTo(0));
            Assert.That(AssetDatabase.GetMainAssetTypeAtPath(dto.Path), Is.Not.Null);
        }

        [Test]
        public void CreateConfig_PathWithoutPrefixOrExtension_IsCompletedUnderAssets()
        {
            var dto = Create(k_Folder.Substring("Assets/".Length) + "/Nested/game");

            Assert.That(dto.Path, Is.EqualTo(k_Folder + "/Nested/game.rc"));
            Assert.That(File.Exists(dto.Path), Is.True);
        }

        [Test]
        public void CreateConfig_SamePathTwice_KeepsTheFirstFile()
        {
            var first = Create(k_Folder + "/twice.rc");
            var second = Create(k_Folder + "/twice.rc");

            Assert.That(second.Path, Is.Not.EqualTo(first.Path));
            Assert.That(File.Exists(first.Path), Is.True);
            Assert.That(second.Path, Does.EndWith(".rc"));
        }

        [Test]
        public void CreateConfig_PathWithParentSegment_IsRejected()
        {
            Assert.Throws<ArgumentException>(() => RemoteConfigEditorCliCommands.CreateConfig("../outside"));
        }

        [Test]
        public void CreateConfig_Entries_WritesOnlyTypesAReaderCannotRecover()
        {
            var dto = Create(k_Folder + "/inferred",
                "{\"name\":\"arena\",\"turn_timer\":30,\"enabled\":true,\"payout\":1.5,\"big\":3000000000,\"rewards\":[{\"id\":\"first_win\"}]}");

            Assert.That(dto.Keys, Is.EqualTo(new[] { "name", "turn_timer", "enabled", "payout", "big", "rewards" }));
            Assert.That(dto.Types, Is.EquivalentTo(new Dictionary<string, string> { ["payout"] = "FLOAT", ["big"] = "LONG" }));
            var document = Read(dto.Path);
            Assert.That(document["entries"]?["turn_timer"]?.Value<int>(), Is.EqualTo(30));
            Assert.That(document["types"]?["payout"]?.Value<string>(), Is.EqualTo("FLOAT"));
            Assert.That(document["types"]?["big"]?.Value<string>(), Is.EqualTo("LONG"));
            Assert.That(document["types"]?["turn_timer"], Is.Null);
        }

        [Test]
        public void CreateConfig_TypeOverride_IsWrittenEvenWhenInferable()
        {
            var dto = Create(k_Folder + "/declared", "{\"payout\":2,\"name\":\"x\"}", "{\"payout\":\"float\",\"name\":\"STRING\"}");

            Assert.That(dto.Types["payout"], Is.EqualTo("FLOAT"));
            Assert.That(dto.Types["name"], Is.EqualTo("STRING"));
        }

        [TestCase("not json")]
        [TestCase("[1,2]")]
        [TestCase("{\"\":1}")]
        [TestCase("{\"key\":null}")]
        [TestCase("{\"key\":1} trailing")]
        public void CreateConfig_InvalidEntries_IsRejected(string entries)
        {
            Assert.Throws<ArgumentException>(() => RemoteConfigEditorCliCommands.CreateConfig(k_Folder + "/bad", entries));
            Assert.That(File.Exists(k_Folder + "/bad.rc"), Is.False);
        }

        [TestCase("{\"key\":1}", "{\"key\":\"DOUBLE\"}")]
        [TestCase("{\"key\":1}", "{\"other\":\"INT\"}")]
        [TestCase("{\"key\":\"text\"}", "{\"key\":\"INT\"}")]
        [TestCase("{\"key\":1}", "[\"INT\"]")]
        [TestCase("{\"key\":1}", "{\"key\":\"3\"}")]
        [TestCase("{\"key\":1}", "{\"key\":\"INT,BOOL\"}")]
        [TestCase("{\"key\":1.5}", "{\"key\":\"INT\"}")]
        [TestCase("{\"key\":3000000000}", "{\"key\":\"INT\"}")]
        [TestCase("{\"key\":1.5}", "{\"key\":\"LONG\"}")]
        [TestCase("{\"key\":99999999999999999999}", "{\"key\":\"LONG\"}")]
        public void CreateConfig_InvalidTypes_IsRejected(string entries, string types)
        {
            Assert.Throws<ArgumentException>(() => RemoteConfigEditorCliCommands.CreateConfig(k_Folder + "/bad", entries, types));
            Assert.That(File.Exists(k_Folder + "/bad.rc"), Is.False);
        }

        [Test]
        public void CreateConfig_TimestampLikeString_IsWrittenUnchanged()
        {
            const string timestamp = "2026-10-28T09:30:00+02:00";
            var dto = Create(k_Folder + "/dated", "{\"season_start\":\"" + timestamp + "\"}");

            Assert.That(dto.Types, Is.Empty);
            Assert.That(File.ReadAllText(dto.Path), Does.Contain("\"" + timestamp + "\""));
        }

        [Test]
        public void CreateConfig_UndeclaredIntegerBeyondLong_IsRejected()
        {
            Assert.Throws<ArgumentException>(() => RemoteConfigEditorCliCommands.CreateConfig(k_Folder + "/huge", "{\"x\":99999999999999999999}"));
            Assert.That(File.Exists(k_Folder + "/huge.rc"), Is.False);
        }

        [Test]
        public void CreateConfig_IntAtTheEdgeOfItsRange_IsAccepted()
        {
            var dto = Create(k_Folder + "/edge", "{\"max\":2147483647,\"min\":-2147483648}", "{\"max\":\"INT\",\"min\":\"INT\"}");

            Assert.That(dto.Types["max"], Is.EqualTo("INT"));
            Assert.That(dto.Types["min"], Is.EqualTo("INT"));
        }
    }
}
#endif
