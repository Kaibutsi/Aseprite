using System.Runtime.InteropServices;

namespace Aseprite
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    unsafe struct Chunk
    {
        public uint      Size;
        public ChunkType Type;

        public readonly bool IsTags  => Type == ChunkType.FrameTags;
        public readonly bool IsLayer => Type == ChunkType.Layer;
        public readonly bool IsCell  => Type == ChunkType.Cel;

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct Tag
        {
            public ushort Start;
            public ushort End;

            public AnimationDirection Direction;

            fixed  byte P2[8];
            public byte R, G, B, A;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct Layer
        {
            public LayerFlags     Flags;
            public LayerType      Type;
            public ushort         ChildLevel;
            public ushort         Width;
            public ushort         Height;
            public LayerBlendMode Blend;
            public byte           Opacity;
            fixed  byte           P1[3];
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct Cel
        {
            public ushort  Layer;
            public short   X;
            public short   Y;
            public byte    Opacity;
            public CelType Type;
            fixed  byte    P1[7];
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    unsafe struct Header
    {
        public uint   FileSize;
        public ushort Magic;
        public ushort Frames;
        public ushort Width;
        public ushort Height;
        public ushort BPP;
        public uint   Flags;
        public ushort Speed;
        fixed  uint   P1[2];
        public byte   PaletteEntry;
        fixed  byte   P2[3];
        public ushort NumberOfColors;
        public byte   PixelWidth;
        public byte   PixelHeight;
        public short  X;
        public short  Y;
        public ushort GridWidth;
        public ushort GridHeight;
        fixed  byte   P3[84];

        public readonly bool EnsureMagic => Magic == 0xA5E0;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    unsafe struct FrameInfo
    {
        public uint   Size;
        public ushort Magic;
        public ushort OldChunks;
        public ushort Duration;
        fixed  byte   P1[2];
        public uint   Chunks;

        public readonly bool EnsureMagic => Magic == 0xF1FA;
    }
}