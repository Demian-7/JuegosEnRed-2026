#if UNITY_PIPELINE_AVAILABLE
using System.Linq;
using NUnit.Framework;
using Unity.Pipeline.Commands;

namespace Unity.Services.RemoteConfig.Cli.Tests
{
    class RemoteConfigCliRegistrationTests
    {
        const string k_CliAssembly = "Unity.Services.RemoteConfig.Cli";

#if DEPLOYMENT_API_AVAILABLE_V1_0
        [Test]
        public void DiscoverCommands_RegistersRemoteConfigCreateConfig()
        {
            Assert.That(Registered(), Does.Contain(("services.remoteconfig", "create_config")));
        }

        [Test]
        public void DiscoverCommands_RegistersRemoteConfigExportAndImport()
        {
            var registered = Registered();
            Assert.That(registered, Does.Contain(("services.remoteconfig", "export")));
            Assert.That(registered, Does.Contain(("services.remoteconfig", "import")));
        }

        static (string Group, string Name)[] Registered() => CommandRegistry.DiscoverCommands()
            .Where(command => command.Package == k_CliAssembly)
            .Select(command => (command.Group, command.Name))
            .ToArray();
#endif
    }
}
#endif
