using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Runtime.CompilerServices;

namespace Ase
{
    public unsafe class Aseprite : IDisposable
    {
        public static int      BufferSize           = 1024 * 1024 * 4;
        public static string[] IgnoredLayerNames    = Array.Empty<string>();
        public static bool     ParseInvisibleLayers = false;
        public static string   IgnorePrefix         = "_";

        public Tag[]       Tags   { get; private set; }
        public Frame[]     Frames { get; private set; }
        public List<Layer> Layers { get; }

        public int Width, Height;

        internal byte[] Buffer;
        int             Position;

        Frame CurrentFrame;

        public Aseprite(Span<byte> input)
        {
            Buffer = ArrayPool<byte>.Shared.Rent(BufferSize);
            Layers = new List<Layer>(32);

            var reader = new MemoryReader(Unsafe.AsPointer(ref input[0]));
            ProcessHeader(ref reader);
        }

        public void Dispose()
        {
            ArrayPool<byte>.Shared.Return(Buffer);
            Buffer = null;
        }

        void ProcessHeader(ref MemoryReader reader)
        {
            ref var header = ref reader.Read<Header>();

            Width  = header.Width;
            Height = header.Height;

            Frames = new Frame[header.Frames];
            for (var i = 0; i < Frames.Length; i++)
                Frames[i] = new() {Index = i};

            for (var i = 0; i < header.Frames; i++)
            {
                ref var frame = ref reader.Read<FrameInfo>();
                CurrentFrame          = Frames[i];
                CurrentFrame.Duration = frame.Duration;
                ProcessFrame(ref reader, ref frame);
            }
        }

        void ProcessFrame(ref MemoryReader reader, ref FrameInfo frame)
        {
            for (var i = 0; i < frame.Chunks; i++)
            {
                var     start = reader.Position;
                ref var chunk = ref reader.Read<Chunk>();

                if (chunk.IsLayer) ProcessChunkLayer(ref reader);
                if (chunk.IsTags) ProcessChunkTags(ref reader);
                if (chunk.IsCell) ProcessChunkCell(ref reader);

                reader.Position = (int) (start + chunk.Size);
            }
        }

        void ProcessChunkLayer(ref MemoryReader reader)
        {
            ref var layer = ref reader.Read<Chunk.Layer>();

            var result = new Layer
            {
                Name    = reader.ReadUTF8(),
                Index   = (ushort) Layers.Count,
                Opacity = layer.Opacity / 255f,
                Visible = layer.Flags.HasFlag(LayerFlags.Visible),
                Blend   = layer.Blend,
                Root    = this
            };
            result.Disabled = !result.ShouldParse;
            Layers.Add(result);
        }

        void ProcessChunkTags(ref MemoryReader reader)
        {
            var count = reader.Read<ushort>();
            reader.Position += 8;

            Tags = new Tag[count];

            for (var i = 0; i < count; i++)
            {
                ref var tag  = ref reader.Read<Chunk.Tag>();
                var     name = reader.ReadUTF8();

                Tags[i] = new Tag
                {
                    Frames    = Frames[tag.Start..tag.End],
                    Direction = tag.Direction,
                    Color = new Color
                    {
                        R = tag.R,
                        G = tag.G,
                        B = tag.B,
                        A = 255
                    },
                    Name = name
                };
            }
        }

        void ProcessChunkCell(ref MemoryReader reader)
        {
            ref var cel = ref reader.Read<Chunk.Cel>();

            if (cel.Type == CelType.Compressed)
            {
                var (width, height) = reader.Read<(ushort, ushort)>();

                reader.Read<ushort>();

                var result = new Cell
                {
                    Start   = Position,
                    Layer   = Layers[cel.Layer],
                    X       = cel.X,
                    Y       = cel.Y,
                    Width   = width,
                    Height  = height,
                    Opacity = cel.Opacity / 255f
                };

                if (!result.Layer.Disabled)
                {
                    using var deflate = new DeflateStream(new UnmanagedMemoryStream(reader.Current, result.Length), CompressionMode.Decompress, false);
                    deflate.Read(Buffer.AsSpan(result.Start, result.Length));

                    CurrentFrame.Add(result);
                    Position += result.Length;
                }
            }

            if (cel.Type == CelType.Linked)
            {
                var position = reader.Read<ushort>();
                var layer    = Layers[cel.Layer];

                if (layer.Disabled) return;
                var source = Frames[position].Cells[CurrentFrame.Cells.Count];

                CurrentFrame.Add(new Cell
                {
                    Start   = source.Start,
                    Layer   = layer,
                    X       = cel.X,
                    Y       = cel.Y,
                    Width   = source.Width,
                    Height  = source.Height,
                    Opacity = cel.Opacity / 255f
                });
            }
        }
    }
}