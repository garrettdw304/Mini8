namespace Mini8
{
    public class Mini8
    {
        public const byte CONTEXT_BITS = 0b0000_0011;
        public const byte ZERO_BIT = 0b1000_0000;
        public const byte CARRY_BIT = 0b0010_0000;
        public const byte OVERFLOW_BIT = 0b0100_0000;
        public const byte CONTEXT_A = 0;
        public const byte CONTEXT_X = 1;
        public const byte CONTEXT_STACK = 2;
        public const byte CONTEXT_FLAGS = 3; // TODO: Should context allow flags. What if we iml and overwrite context to different reg?

        private readonly MemoryMap mem;
        private readonly Instruction[] instructions; private int instOffset = 0;

        private byte a;
        private byte x;
        private byte stack;
        private byte page; // TODO: Utilize
        private byte flags;
        private ushort pc;
        private byte tempContext;
        /// <summary>
        /// True if tempContext applies to the next instruction.
        /// </summary>
        private bool tempContextActive;
        /// <summary>
        /// True if tempContext was set this instruction.
        /// </summary>
        private bool tempContextActivated;
        private byte Context
        {
            get => (byte)(flags & CONTEXT_BITS);
            set => flags = (byte)((flags & ~CONTEXT_BITS) | value);
        }
        private bool Zero
        {
            get => (flags & ZERO_BIT) != 0;
            set => flags =
                (byte)(value ? (flags | ZERO_BIT) : (flags & ~ZERO_BIT));
        }
        private bool Carry // TODO: Utilize
        {
            get => (flags & CARRY_BIT) != 0;
            set => flags =
                (byte)(value ? (flags | CARRY_BIT) : (flags & ~CARRY_BIT));
        }
        private bool Overflow // TODO: Utilize
        {
            get => (flags & OVERFLOW_BIT) != 0;
            set => flags =
                (byte)(value ?
                    (flags | OVERFLOW_BIT)
                    :
                    (flags & ~OVERFLOW_BIT));
        }
        /// <summary>
        /// Acts inplace of a, x, stack or flags based on context.
        /// </summary>
        private byte C
        {
            get
            {
                byte context = tempContextActive ? tempContext : Context;
                return context switch
                {
                    CONTEXT_A => a,
                    CONTEXT_X => x,
                    CONTEXT_STACK => stack,
                    CONTEXT_FLAGS => flags,
                    _ =>throw new InvalidOperationException(
                        "Context is not a valid value."),
                };
            }

            set
            {
                byte context = tempContextActive ? tempContext : Context;
                switch (context)
                {
                    case CONTEXT_A: a = value; break;
                    case CONTEXT_X: x = value; break;
                    case CONTEXT_STACK: stack = value; break;
                    case CONTEXT_FLAGS: flags = value; break;
                    default: throw new InvalidOperationException(
                        "Context is not a valid value.");
                }
            }
        }

        public Mini8(MemoryMap mem)
        {
            this.mem = mem;
            pc = a = x = stack = page = flags = tempContext = 0;
            tempContextActive = tempContextActivated = false;

            instructions = new Instruction[256];
            Set16(Iml, Imh, Nand, Nor, Xnor, Add, Sub, Bi, Je, Cal, Sti, Ldi,
                Ste, Lde);
            Set1(Shl, Shr, Push, Pop, Mac, Mxc, Msc, Mpc, Mfc, Mca, Mcx, Mcs,
                Mcp, Mcf, Ina, Inx, Dea, Dex, Ca, Cx, Cs, Cf, Cta, Ctx, Cts,
                Ctf, Ze, Zo, Zc, Zn, Ret,  Nop);
        }

        public void Step()
        {
            byte instructionByte = mem.Load(pc);
            
            try
            {
                instructions[instructionByte](instructionByte);
            } catch (Exception e)
            {
                Console.Error.WriteLine(e.ToString());
            }

            if (tempContextActivated)
                tempContextActivated = false;
            else
                tempContextActive = false;

            pc++;
        }

        #region Instructions
        private void Nop(byte instruction)
        {
            
        }

        private void Iml(byte instruction)
        {
            C = (byte)((C & 0b1111_0000) | (instruction & 0b0000_1111));
        }

        private void Imh(byte instruction)
        {
            C = (byte)((C & 0b0000_1111) | (instruction << 4));
        }

        #region Transfer
        private void Push(byte instruction)
        {
            mem.Store(stack++, C);
        }

        private void Pop(byte instruction)
        {
            C = mem.Load(--stack);
        }

        private void Mac(byte instruction)
        {
            C = a;
        }

        private void Mxc(byte instruction)
        {
            C = x;
        }

        private void Msc(byte instruction)
        {
            C = stack;
        }

        private void Mpc(byte instruction)
        {
            C = page;
        }

        private void Mfc(byte instruction)
        {
            C = flags;
        }

        private void Mca(byte instruction)
        {
            a = C;
        }

        private void Mcx(byte instruction)
        {
            x = C;
        }

        private void Mcs(byte instruction)
        {
            stack = C;
        }

        private void Mcp(byte instruction)
        {
            page = C;
        }

        private void Mcf(byte instruction)
        {
            flags = C;
        }

        private void Sti(byte instruction)
        {
            mem.Store((ushort)(instruction & 0b1111), C);
        }

        private void Ldi(byte instruction)
        {
            C = mem.Load((ushort)(instruction & 0b1111));
            Zero = C == 0;
        }

        private void Ste(byte instruction)
        {
            ushort address = (ushort)((instruction << 8) | x);
            address &= 0x0F_FF;

            mem.Store(address, C);
        }

        private void Lde(byte instruction)
        {
            ushort address = (ushort)((instruction << 8) | x);
            address &= 0x0F_FF;

            C = mem.Load(address);
            Zero = C == 0;
        }
        #endregion Transfer

        #region Arithmetic
        private void Nand(byte instruction)
        {
            a = (byte)~(a & GetRhs(instruction));
            Zero = a == 0;
        }

        private void Nor(byte instruction)
        {
            a = (byte)~(a | GetRhs(instruction));
            Zero = a == 0;
        }

        private void Xnor(byte instruction)
        {
            a = (byte)~(a ^ GetRhs(instruction));
            Zero = a == 0;
        }

        private void Add(byte instruction)
        {
            a += GetRhs(instruction);
            Zero = a == 0;
        }

        private void Sub(byte instruction)
        {
            a -= GetRhs(instruction);
            Zero = a == 0;
        }

        private void Shl(byte instruction)
        {
            a <<= a;
            Zero = a == 0;
        }

        private void Shr(byte instruction)
        {
            a >>= a;
            Zero = a == 0;
        }

        private void Ina(byte instruction)
        {
            a++;
            Zero = a == 0;
        }

        private void Inx(byte instruction)
        {
            x++;
            Zero = x == 0;
        }

        private void Dea(byte instruction)
        {
            a--;
            Zero = a == 0;
        }

        private void Dex(byte instruction)
        {
            x--;
            Zero = x == 0;
        }
        #endregion Arithmetic

        #region Context
        private void Ca(byte instruction)
        {
            Context = CONTEXT_A;
        }

        private void Cx(byte instruction)
        {
            Context = CONTEXT_X;
        }

        private void Cs(byte instruction)
        {
            Context = CONTEXT_STACK;
        }

        private void Cf(byte instruction)
        {
            Context = CONTEXT_FLAGS;
        }

        private void Cta(byte instruction)
        {
            tempContext = CONTEXT_A;
            tempContextActive = tempContextActivated = true;
        }

        private void Ctx(byte instruction)
        {
            tempContext = CONTEXT_X;
            tempContextActive = tempContextActivated = true;
        }

        private void Cts(byte instruction)
        {
            tempContext = CONTEXT_STACK;
            tempContextActive = tempContextActivated = true;
        }

        private void Ctf(byte instruction)
        {
            tempContext = CONTEXT_FLAGS;
            tempContextActive = tempContextActivated = true;
        }
        #endregion Context

        #region Branching and Conditions
        private void Ze(byte instruction)
        {
            Zero = true;
        }

        private void Zo(byte instruction)
        {
            Zero = Overflow;
        }

        private void Zc(byte instruction)
        {
            Zero = Carry;
        }

        private void Zn(byte instruction)
        {
            Zero = !Zero;
        }

        // Rule for encoding this instruction:
        // If jumping forward, take number to jump and subtract 1
        //      (because all positive values are offset by 1, 1 will skip next 2 instructions, 0 will skip next 1 instruction etc)
        // If jumping backward, take number to jump and make it negative
        //      (so -1 will run this instruction again, -2 will run the instruction before this one)
        private void Bi(byte instruction)
        {
            if (!Zero)
                return;

            short offset = GetImmOffset(instruction);
            pc = (ushort)(pc + offset);
        }

        private void Je(byte instruction)
        {
            if (!Zero)
                return;

            ushort address = (ushort)((instruction << 8) | x);
            address &= 0x0F_FF;

            // -1 because of pc increment at end of instruction.
            pc = (ushort)(address - 1);
        }

        private void Cal(byte instruction)
        {
            PushForJump();
            ushort address = (ushort)((instruction << 8) | x);
            address &= 0x0F_FF;

            // -1 because of pc increment at end of instruction.
            pc = (ushort)(address - 1);
        }

        private void Ret(byte instruction)
        {
            PopForReturn();
        }
        #endregion Branching and Conditions
        #endregion Instructions

        private void PushForJump()
        {
            mem.Store(stack++, flags);
            mem.Store(stack++, (byte)pc);
            mem.Store(stack++, (byte)(pc >> 8));
        }

        private void PopForReturn()
        {
            pc = (ushort)((mem.Load(--stack) << 8) | mem.Load(--stack));
            flags = mem.Load(--stack);
        }

        private byte GetRhs(byte instruction)
        {
            return mem.Load((ushort)(instruction & 0b0000_1111));
        }

        private static short GetImmOffset(byte instruction)
        {
            short offset = (byte)(instruction & 0b0000_1111);

            // sign extend for a 4 bit signed number to a 16 bit signed number.
            if ((offset | 0b0000_1000) != 0)
                offset = (short)(offset | 0xFF_F0);

            if (offset >= 0)
                offset++;

            return offset;
        }

        private void Set16(params Instruction[] instruction)
        {
            for (int inst = 0; inst < instruction.Length; inst++)
            {
                int stop = instOffset + 16;
                for (; instOffset < stop; instOffset++)
                    instructions[instOffset] = instruction[inst];
            }
        }

        private void Set1(params Instruction[] instruction)
        {
            for (int i = 0; i < instruction.Length; i++)
                instructions[instOffset++] = instruction[i];
        }

        private delegate void Instruction(byte instruction);
    }
}
