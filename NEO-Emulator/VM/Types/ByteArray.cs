﻿using System.Linq;
using Neo.VM;

namespace Neo_Emulator.VM.Types
{
    public class ByteArray : StackItem
    {
        private byte[] value;

        public ByteArray(byte[] value)
        {
            this.value = value;
        }

        public override bool Equals(StackItem other)
        {
            if (ReferenceEquals(this, other)) return true;
            if (ReferenceEquals(null, other)) return false;
            return value.SequenceEqual(other.GetByteArray());
        }

        public override bool GetBoolean()
        {
            return value != null && value[0] != 0;
        }

        public override byte[] GetByteArray()
        {
            return value;
        }
    }
}
