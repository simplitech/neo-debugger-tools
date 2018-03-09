using Neo.SmartContract.Framework.Services.Neo;
using Neo.SmartContract.Framework.Services.System;
using Neo.SmartContract.Framework;
using System.Numerics;
using System;

namespace Neo.SmartContract
{
    public class Contract1 : Framework.SmartContract
    {
        public static Object Main(string operation, params object[] args)
        {
            return "Hello world";
        }
    }
}