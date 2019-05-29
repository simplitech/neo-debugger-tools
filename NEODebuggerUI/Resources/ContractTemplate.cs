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
            if (operation == "name")
            {
                return Name();
            }
            else if (operation == "symbol")
            {
                return Symbol();
            }
            else if (operation == "decimals")
            {
                return Decimals();
            }else if (operation == "deploy")
            {
                return Deploy();
            }

            return false;
        }

        public static string Deploy()
        {
            string testVariable = "Ricardo";
            //if (Runtime.CheckWitness(ContractOwner))
            //{
            //    return "Not Authorized"; 
            //}

            BigInteger totalSupply = Storage.Get(Storage.CurrentContext, KeyTotalSupply).AsBigInteger();

            if(totalSupply == 0)
             {
                Storage.Put(Storage.CurrentContext, KeyTotalSupply, ContractOwner);
             }

            totalSupply = Storage.Get(Storage.CurrentContext, KeyTotalSupply).AsBigInteger();

            return "success";
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
