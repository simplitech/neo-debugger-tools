using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
	}
}
