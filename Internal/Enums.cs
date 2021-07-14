namespace Aseprite
{
    enum LayerBlendMode : ushort
    {
        Normal     = 0,
        Multiply   = 1,
        Screen     = 2,
        Overlay    = 3,
        Darken     = 4,
        Lighten    = 5,
        ColorDodge = 6,
        ColorBurn  = 7,
        HardLight  = 8,
        SoftLight  = 9,
        Difference = 10,
        Exclusion  = 11,
        Hue        = 12,
        Saturation = 13,
        Color      = 14,
        Luminosity = 15,
        Addition   = 16,
        Subtract   = 17,
        Divide     = 18
    }

    enum ChunkType : ushort
    {
        OldPaletteA = 0x0004,
        OldPaletteB = 0x0011,
        Layer       = 0x2004,
        Cel         = 0x2005,
        CelExtra    = 0x2006,
        Mask        = 0x2016,
        Path        = 0x2017,
        FrameTags   = 0x2018,
        Palette     = 0x2019,
        UserData    = 0x2020,
        Slice       = 0x2022
    }

    enum CelType : ushort
    {
        Raw = 0,
        Linked,
        Compressed
    }

    enum LayerType : ushort
    {
        Normal,
        Group
    }

    enum LayerFlags : ushort
    {
        Visible      = 0b00000001,
        Editable     = 0b00000010,
        Locked       = 0b00000100,
        Background   = 0b00001000,
        PreferLinked = 0b00010000,
        Collapsed    = 0b00100000,
        Reference    = 0b01000000
    }
}