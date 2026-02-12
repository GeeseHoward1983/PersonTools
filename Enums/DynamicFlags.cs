namespace PersonalTools.Enums
{
    [Flags]
    internal enum DynamicOptions : uint
    {
        DF_ORIGIN = 0x00000001,         // Object may use DF_ORIGIN
        DF_SYMBOLIC = 0x00000002,       // Symbol resolutions starts here
        DF_TEXTREL = 0x00000004,        // Object contains text relocations
        DF_BIND_NOW = 0x00000008,       // Don't lazy bind
        DF_STATIC_TLS = 0x00000010      // Static thread local storage
    }
}