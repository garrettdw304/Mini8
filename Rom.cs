using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mini8
{
    public class Rom : MemoryMap.IDevice
    {
        private readonly ushort baseAddress;
        private readonly ushort length;
        private byte[] data;
        public Rom(ushort baseAddress, ushort length)
        {
            data = new byte[length];
        }

        public bool MappedTo(ushort address)
        {
            return address >= baseAddress && address < baseAddress + length;
        }

        public byte Load(ushort address)
        {
            if (!MappedTo(address)) throw new ArgumentException();

            return data[address];
        }

        public void Store(ushort address, byte value)
        {
            throw new InvalidOperationException();
        }

        public void Program(byte[] data)
        {
            for (int i = 0; i < data.Length; i++)
            {
                this.data[i] = data[i];
            }
        }
    }
}
