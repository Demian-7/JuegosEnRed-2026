namespace Unity.Services.RemoteConfig.Cli
{
    /// <summary>
    /// The constants this package's CLI commands use: their group and tags, so each has one source of truth.
    /// Keep one class per package: a const read from another assembly is compiled into the reader, so a
    /// shared class would go stale in every package not rebuilt with it.
    /// Each command carries both the cascading <see cref="Service"/> path and the flat
    /// <see cref="RemoteConfig"/>: the segment-aware <c>--tag</c> filter reaches a command through
    /// <c>services</c> / <c>services/remoteconfig</c> via the path, but only through
    /// <c>remoteconfig</c> via the flat tag.
    /// </summary>
    static class RemoteConfigCliConstants
    {
        public const string Group = "services.remoteconfig";
        public const string Service = "services/remoteconfig";
        public const string RemoteConfig = "remoteconfig";
    }
}
