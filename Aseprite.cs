using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Runtime.CompilerServices;

namespace Aseprite
{
    public unsafe class Aseprite
    {
        public static int      BufferSize           = 1024 * 1024 * 4;
        public static string[] IgnoredLayerNames    = Array.Empty<string>();
        public static bool     ParseInvisibleLayers = false;

        public Tag[]       Tags   { get; private set; }
        public Frame[]     Frames { get; private set; }
        public List<Layer> Layers { get; }

        public int Width, Height;

        byte[] Buffer;
        int    Position;

        Frame         CurrentFrame;
        DeflateStream Deflate;

        public Aseprite(Span<byte> input)
        {
            Buffer = ArrayPool<byte>.Shared.Rent(BufferSize);
            Layers = new List<Layer>(32);

            var reader = new MemoryReader(Unsafe.AsPointer(ref input[0]));

            fixed (byte* ptr = &input.GetPinnableReference())
                Deflate = new DeflateStream(new UnmanagedMemoryStream(ptr, input.Length), CompressionMode.Decompress, true);

            ProcessHeader(ref reader);

            Deflate.BaseStream.Dispose();
            Deflate.Dispose();
        }

        void ProcessHeader(ref MemoryReader reader)
        {
            ref var header = ref reader.Read<Header>();

            Width  = header.Width;
            Height = header.Height;

            Frames = new Frame[header.Frames];
            for (var i = 0; i < Frames.Length; i++)
                Frames[i] = new Frame {Root = this};

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
                Opacity = layer.Opacity,
                Visible = layer.Flags.HasFlag(LayerFlags.Visible),
                Blend   = layer.Blend,
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
                Deflate.BaseStream.Position = reader.Position;

                var result = new Cell
                {
                    Width   = width,
                    Height  = height,
                    Start   = Position,
                    Opacity = cel.Opacity / 255f,
                    Layer   = Layers[cel.Layer],
                    X       = cel.X,
                    Y       = cel.Y
                };

                if (!result.Layer.Disabled)
                {
                    Deflate.Read(Buffer.AsSpan(result.Start, result.Length));
                    CurrentFrame.Add(result);
                    Position += result.Length;
                }
            }

            if (cel.Type == CelType.Linked)
            {
                var position = reader.Read<ushort>();
                var source   = Frames[position].LastCell;

                if (Layers[cel.Layer].Disabled) return;

                CurrentFrame.Add(new Cell
                {
                    Layer   = Layers[cel.Layer],
                    Opacity = source.Opacity,
                    X       = cel.X,
                    Y       = cel.Y,
                    Width   = source.Width,
                    Height  = source.Height,

                    Start = source.Start
                });
            }
        }
    }
}