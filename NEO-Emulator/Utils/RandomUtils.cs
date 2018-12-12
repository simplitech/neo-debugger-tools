using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Neo.Lux.Cryptography;

namespace NEO_Emulator.Utils
{
	class RandomUtils
	{
		private static Random _random;

		public static uint RandomUInt()
		{
			if (_random == null)
			{
				_random = new Random();
			}

			return (uint)_random.Next(int.MaxValue);
		}

        public static UInt256 RandomHash()
        {
            byte[] array = new byte[32];
            _random = new Random();
            _random.NextBytes(array);
            return new UInt256(array);
        }
    }
}
