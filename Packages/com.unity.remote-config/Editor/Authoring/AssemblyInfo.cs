using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Unity.Services.RemoteConfig.Cli")]

#if UNITY_INCLUDE_TESTS
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]
[assembly: InternalsVisibleTo("Unity.Services.RemoteConfig.Tests.Editor.Authoring")]
#endif
