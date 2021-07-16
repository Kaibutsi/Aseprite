using System.Text;

namespace Ase
{
    unsafe ref struct MemoryReader
    {
        byte*      ptr;
        public int Position;

        public byte* Current => ptr + Position;

        public MemoryReader(void* address)
        {
            ptr      = (byte*) address;
            Position = 0;
        }

        public ref T Read<T>() where T : unmanaged
        {
            ref var current = ref *(T*) (ptr + Position);
            Position += sizeof(T);
            return ref current;
        }

        public ref T Peak<T>() where T : unmanaged => ref *(T*) (ptr + Position);

        public string ReadUTF8()
        {
            var length = Read<ushort>();
            var result = Encoding.UTF8.GetString(ptr + Position, length);
            Position += length;
            return result;
        }
    }
}