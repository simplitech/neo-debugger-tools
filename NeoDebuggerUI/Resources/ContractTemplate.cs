using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services.Neo;
using System;
using System.Numerics;



namespace Example
{
    public class ContractExample : SmartContract
    {

        /// <summary>
        /// Smart Contract entry point. 
        /// Default SC signature: (operation: String, args: object[]) : object 
        /// Available types: object, byte[], byte, BigInteger, string, object[] and map.
        /// Reference: https://docs.neo.org/en-us/sc/introduction.html 
        /// </summary>
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
