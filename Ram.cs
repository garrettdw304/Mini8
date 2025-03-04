using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mini8
{
    public class Ram : MemoryMap.IDevice
    {
        private readonly ushort baseAddress;
        private readonly ushort length;
        private byte[] data;
        public Ram(ushort baseAddress, ushort length)
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
            if (!MappedTo(address)) throw new ArgumentException();

            data[address] = value;
        }
    }
}
