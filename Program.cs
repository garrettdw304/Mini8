namespace Mini8
{
    internal class Program
    {
        static void Main(string[] args)
        {
            /*
             * RAM      000 - DFD       3582
             * UART     DFE - DFF       2
             * ROM      E00 - FFF       512
             */
            const int RAM_BASE =        0x0;
            const int RAM_LENGTH =              3582;
            const int UART_BASE =       0xDFE;
            const int ROM_BASE =        0xE00;
            const int ROM_LENGTH =              512;

            MemoryMap mm = new MemoryMap();
            Rom rom = new Rom(ROM_BASE, ROM_LENGTH);
            mm.AddDevice(rom);
            Ram ram = new Ram(RAM_BASE, RAM_LENGTH);
            mm.AddDevice(ram);
            Uart uart = new Uart(UART_BASE, null);

            Mini8 mini = new Mini8(mm);
            new Thread(() =>
            {
                while (true)
                {
                    mini.Step();
                    Thread.Sleep(5); // 5ms sleep -> 200Hz
                }
            }).Start();

            while (true)
            {
                if (Console.In.Peek() != -1)
                    uart.Send((byte)Console.In.Read());
                if (uart.CanReceive())
                    Console.Out.Write(uart.Receive());
            }
        }
    }
}
