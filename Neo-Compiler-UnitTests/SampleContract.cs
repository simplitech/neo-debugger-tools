using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services.Neo;
using System;
using System.Numerics;


namespace Example
{
    public class Calculator  : SmartContract
    {
        public static object Main(string operation, params object[] args)
        {
            switch (operation)
            {
                case "symbol":
                    {
                        return "COZ";
                    }

                case "write":
                    {
                        var key = (string)args[0];
                        var data = (string)args[1];

                        Storage.Put(Storage.CurrentContext, key, data);

                        return true;
                    }

                case "read":
                    {
                        var key = (string)args[0];

                        var data = Storage.Get(Storage.CurrentContext, key);

                        if (data == null)
                        {
                            return false;
                        }

                        Runtime.Notify(data.AsString());
                        return true;
                    }

                default: return false;
            }
        }
    }
}
