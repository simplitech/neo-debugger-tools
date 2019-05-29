using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services.Neo;
using System;
using System.Numerics;



namespace Example
{
    public class ContractExample : SmartContract
    {
        public static readonly string KeyBalancePrefix = "b_";
        public static readonly string KeyTotalSupply = "total_supply";
        public static readonly byte[] ContractOwner = "Ad83tfsuWxxexhefPzXVpn5vv6oCbLKFEx".ToScriptHash();
        /// <summary>
        /// Smart Contract entry point. 
        /// Default SC signature: (operation: String, args: object[]) : object 
        /// Available types: object, byte[], byte, BigInteger, string, object[] and map.
        /// Reference: https://docs.neo.org/en-us/sc/introduction.html 
        /// </summary>
        public static object Main(string operation, params object[] args)
        {
            object returnedValue = null;

            if (operation == "name")
            {
                returnedValue = Name();
            }
            else if (operation == "symbol")
            {
                returnedValue = Symbol();
            }
            else if (operation == "decimals")
            {
                returnedValue = Decimals();
            }else if (operation == "deploy")
            {
                returnedValue = Deploy();
            }

            return returnedValue;
        }

        public static string Deploy()
        {
            byte[] owner = "Ad83tfsuWxxexhefPzXVpn5vv6oCbLKFEx".ToScriptHash();
            if (Runtime.CheckWitness(owner))
            {
                return "Not Authorized"; 
            }
            BigInteger totalSupply = Storage.Get(Storage.CurrentContext, KeyTotalSupply).AsBigInteger();
            if(totalSupply == 0)
             {
                
             }
            
            return "Success";
        }

        public static string Name()
        {
            return "NEP-5 Template";
        }

        public static string Symbol()
        {
            return "NEP5";
        }

        public static BigInteger Decimals()
        {
            return 8;
        }
    }
}
