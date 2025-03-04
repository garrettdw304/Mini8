using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mini8
{
    public class MemoryMap
    {
        /// <summary>
        /// Throws an exception if no devices respond to a load.
        /// </summary>
        public readonly bool alertOnFloatingResult;
        /// <summary>
        /// Throws an exception if more than one device responds to a load.
        /// </summary>
        public readonly bool alertOnBusContention;
        /// <summary>
        /// Throws an exception if no device uses a value from a store.
        /// </summary>
        public readonly bool alertOnNoReceiver;
        /// <summary>
        /// Throws an exception if more than one device uses a value from a store.
        /// </summary>
        public readonly bool alertOnMultipleReceivers;

        private readonly List<IDevice> devices;

        public MemoryMap(bool alertOnFloatingResult = true, bool alertOnBusContention = true, bool alertOnNoReceiver = true, bool alertOnMultipleReceivers = true)
        {
            this.alertOnFloatingResult = alertOnFloatingResult;
            this.alertOnBusContention = alertOnBusContention;
            this.alertOnNoReceiver = alertOnNoReceiver;
            this.alertOnMultipleReceivers = alertOnMultipleReceivers;
            devices = new List<IDevice>();
        }

        public void AddDevice(IDevice device)
        {
            devices.Add(device);
        }

        public byte Load(ushort address)
        {
            byte result = 0;
            List<IDevice> responders = new List<IDevice>();
            foreach (IDevice d in devices)
                if (d.MappedTo(address))
                {
                    responders.Add(d);
                    result |= d.Load(address);
                }

            if (responders.Count == 1)
                return result;
            else if (responders.Count == 0)
            {
                if (alertOnFloatingResult)
                    throw new FloatingResultException(result, responders);
                return (byte)Random.Shared.Next(); // Floating (just picks random bits)
            }
            else
            {
                if (alertOnBusContention)
                    throw new BusContentionException(result, responders);
                return result; // Bus Contention (assumes 1s beat 0s)
            }
        }

        public void Store(ushort address, byte value)
        {
            List<IDevice> receivers = new List<IDevice>();
            foreach (IDevice d in devices)
                if (d.MappedTo(address))
                {
                    receivers.Add(d);
                    d.Store(address, value);
                }

            if (receivers.Count == 0 && alertOnNoReceiver)
                throw new NoReceiverException();

            if (receivers.Count > 1 && alertOnMultipleReceivers)
                throw new MultipleReceiversException(receivers);
        }

        public interface IDevice
        {
            public bool MappedTo(ushort address);
            public byte Load(ushort address);
            public void Store(ushort address, byte value);
        }

        public class MemoryMapException : Exception { }

        public class BusContentionException : MemoryMapException
        {
            public readonly byte result;
            public readonly IReadOnlyList<IDevice> devices;
            public BusContentionException(byte result, List<IDevice> devices)
            {
                this.result = result;
                this.devices = devices;
            }
        }

        public class FloatingResultException : MemoryMapException
        {
            public readonly byte result;
            public readonly IReadOnlyList<IDevice> devices;
            public FloatingResultException(byte result, List<IDevice> devices)
            {
                this.result = result;
                this.devices = devices;
            }
        }

        public class NoReceiverException : MemoryMapException { }

        public class MultipleReceiversException : MemoryMapException
        {
            public readonly IReadOnlyList<IDevice> devices;
            public MultipleReceiversException(List<IDevice> devices)
            {
                this.devices = devices;
            }
        }
    }
}
