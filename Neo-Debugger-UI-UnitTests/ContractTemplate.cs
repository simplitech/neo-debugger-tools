using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services.Neo;
using System;
using System.Numerics;



namespace Example
{
    public class ContractExample : SmartContract
    {
        public static object Main(string operation, params object[] args)
        {
            switch (operation)
            {
                case "name":
                    {
                    
                        return "City of Zion";
                    }

                case "symbol":
                    {
                        return "CoZ";
                    }

                default: return false;
            }
        }
    }
}
