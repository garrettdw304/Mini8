using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mini8
{
    public class Uart : MemoryMap.IDevice
    {
        public const int OUT_Q_BUFFER_SIZE = 16;
        public const int IN_Q_BUFFER_SIZE = 16;

        public readonly ushort baseAddress;
        private readonly Action? onReset;

        private readonly ConcurrentQueue<byte> outQ;
        private readonly ConcurrentQueue<byte> inQ;

        public Uart(ushort baseAddress, Action? onReset)
        {
            this.baseAddress = baseAddress;
            this.onReset = onReset;
            outQ = new ConcurrentQueue<byte>();
            inQ = new ConcurrentQueue<byte>();
        }

        public bool MappedTo(ushort address)
        {
            return address == baseAddress || address == (baseAddress + 1);
        }

        public byte Load(ushort address)
        {
            if (!MappedTo(address)) throw new ArgumentException();

            if (address == baseAddress)
            {
                if (inQ.TryDequeue(out byte value))
                    return value;
                else
                    return 0;
            }
            else
            {
                return (byte)(outQ.Count | (inQ.Count << 4));
            }
        }

        public void Store(ushort address, byte value)
        {
            if (!MappedTo(address)) throw new ArgumentException();

            if (address == baseAddress)
            {
                if (outQ.Count <= OUT_Q_BUFFER_SIZE)
                    outQ.Enqueue(value);
            }
            else
            {
                outQ.Clear();
                inQ.Clear();
                onReset?.Invoke();
            }
        }

        public void Send(byte value)
        {
            if (inQ.Count <= OUT_Q_BUFFER_SIZE)
                inQ.Enqueue(value);
        }

        public byte Receive()
        {
            if (outQ.TryDequeue(out byte value))
                return value;
            else
                return 0;
        }

        public bool CanReceive()
        {
            return !outQ.IsEmpty;
        }
    }
}
