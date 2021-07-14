using System.Collections.Generic;
using System.Linq;

namespace Aseprite
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
    }

    public class Tag
    {
        public string Name;
        public Color  Color;

        public Frame[] Frames;

        public AnimationDirection Direction;
    }

    public class Frame
    {
        public int Duration;

        public int MinX = int.MaxValue,
                   MinY = int.MaxValue,
                   MaxX = int.MinValue,
                   MaxY = int.MinValue;

        public int X      => MinX;
        public int Y      => MinY;
        public int Width  => MaxX - MinX;
        public int Height => MaxY - MinY;

        public List<Cell> Cells { get; private set; }

        internal Aseprite Root;

        internal Cell LastCell => Cells?.LastOrDefault();

        internal void Add(Cell cell)
        {
            if (cell.X               < MinX) MinX = cell.X;
            if (cell.Y               < MinY) MinY = cell.Y;
            if (cell.X + cell.Width  > MaxX) MaxX = cell.X + cell.Width;
            if (cell.Y + cell.Height > MaxY) MaxY = cell.Y + cell.Height;

            (Cells ??= new List<Cell>(16)).Add(cell);
        }
    }

    public class Layer
    {
        public string Name;
        public ushort Index;
        public byte   Opacity;
        public bool   Visible;

        internal LayerBlendMode Blend;

        internal bool Disabled;

        internal bool ShouldParse
        {
            get
            {
                if (Aseprite.IgnoredLayerNames.Any(a => a == Name)) return false;
                if (Visible) return true;
                return Aseprite.ParseInvisibleLayers;
            }
        }
    }
}