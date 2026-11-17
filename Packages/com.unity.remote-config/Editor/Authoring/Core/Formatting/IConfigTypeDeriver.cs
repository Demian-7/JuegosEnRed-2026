namespace Unity.Services.RemoteConfig.Editor.Authoring.Core.Formatting
{
    /// <summary>
    /// The type for a given entry in a RemoteConfigEntry
    /// </summary>
    // ReSharper disable InconsistentNaming - Used in serialization
    public enum ConfigType
    {
        /// <summary>
        /// A value of string type
        /// </summary>
        STRING,
        /// <summary>
        /// A value of int type
        /// </summary>
        INT,
        /// <summary>
        /// A value of bool type
        /// </summary>
        BOOL,
        /// <summary>
        /// A value of float type
        /// </summary>
        FLOAT,
        /// <summary>
        /// A value of long type
        /// </summary>
        LONG,
        /// <summary>
        /// A value of object type
        /// </summary>
        JSON
    }

    interface IConfigTypeDeriver
    {
        ConfigType DeriveType(object obj);
    }
}
