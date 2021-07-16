using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ase
{
    public enum AnimationDirection : byte
    {
        Forward,
        Reversed,
        PingPong
    }

    public struct Color
    {
        public byte R, G, B, A;
    }

    public class Tag
    {
        public string             Name;
        public Color              Color;
        public AnimationDirection Direction;

        public Frame[] Frames;

        public int Width => Frames.Length == 0 ? 0 : Frames[0].Root.Width * Frames.Length;

        public void Render<T>(Span<T> buffer, int stride) where T : unmanaged
        {
            foreach (var frame in Frames)
            {
                frame.Render(buffer, stride);
                buffer = buffer[frame.Root.Width..];
            }
        }
    }

    public class Layer
    {
        public string Name;
        public ushort Index;
        public float  Opacity;
        public bool   Visible;

        internal LayerBlendMode Blend;
        internal Aseprite       Root;

        internal bool Disabled;

        internal bool ShouldParse
        {
            get
            {
                if (Aseprite.IgnoredLayerNames.Any(a => a == Name)) return false;
                if (Name.StartsWith(Aseprite.IgnorePrefix)) return false;
                if (Visible) return true;
                return Aseprite.ParseInvisibleLayers;
            }
        }
    }

    public class Frame
    {
        internal Aseprite Root => Cells[0].Layer.Root;

        public int Duration;
        public int Index;

        public int MinX = int.MaxValue,
                   MinY = int.MaxValue,
                   MaxX = int.MinValue,
                   MaxY = int.MinValue;

        public int X      => MinX;
        public int Y      => MinY;
        public int Width  => MaxX - MinX;
        public int Height => MaxY - MinY;

        public List<Cell> Cells { get; } = new(16);

        public void Render<T>(Span<T> buffer, int stride) where T : unmanaged
        {
            foreach (var cell in Cells.OrderBy(a => a.Layer.Index)) cell.Render(buffer, stride);
        }

        internal void Add(Cell cell)
        {
            if (cell.X               < MinX) MinX = cell.X;
            if (cell.Y               < MinY) MinY = cell.Y;
            if (cell.X + cell.Width  > MaxX) MaxX = cell.X + cell.Width;
            if (cell.Y + cell.Height > MaxY) MaxY = cell.Y + cell.Height;

            Cells.Add(cell);
        }
    }

    public class Cell
    {
        public Layer Layer;

        public float Opacity;

        public int X;
        public int Y;

        public int Width;
        public int Height;

        public int Start;
        public int Length => Width * Height * 4;

        // if offset is false then the rendering will start at 0, 0
        public unsafe void Render<T>(Span<T> buffer, int stride, bool offset = true) where T : unmanaged
        {
            if (sizeof(T) != 4)
                throw new Exception($"Aseprite Cell.Render<T>(): The length of {typeof(T)} is not 4 bytes");

            fixed (byte* ptr = Layer.Root.Buffer)
            {
                var off = offset ? (X + Y * stride) : 0;

                var source = new Span<Color>(ptr, Layer.Root.Buffer.Length / 4);
                var target = MemoryMarshal.Cast<T, Color>(buffer)[off..];
                var data   = source.Slice(Start / 4, Width * Height);

                var position = 0;
                var opacity  = (byte) (Layer.Opacity * Opacity * 255);

                foreach (var color in data)
                {
                    Blend(ref target[position], color, opacity);

                    if (position++ == Width - 1)
                    {
                        target   = target[stride..];
                        position = 0;
                    }
                }
            }
        }

        static void Blend(ref Color dest, Color src, byte opacity)
        {
            if (src.A == 0) return;
            if (dest.A == 0) dest = src;
            else
            {
                var sa = Multi(src.A, opacity);
                var ra = dest.A + sa - Multi(dest.A, sa);

                dest.R = (byte) (dest.R + (src.R - dest.R) * sa / ra);
                dest.G = (byte) (dest.G + (src.G - dest.G) * sa / ra);
                dest.B = (byte) (dest.B + (src.B - dest.B) * sa / ra);
                dest.A = (byte) ra;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static int Multi(int a, int b)
        {
            var t = a * b + 0x80;
            return ((t >> 8) + t) >> 8;
        }
    }
}