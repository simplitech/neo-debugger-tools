using Neo.VM;
using Neo.VM.Types;
using NEO_Emulator.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using VMArray = Neo.VM.Types.Array;
using VMBoolean = Neo.VM.Types.Boolean;

namespace Neo.Emulation.API
{
    public class Contract : VM.IInteropInterface
    {
        public byte[] script;

        [Syscall("Neo.Contract.GetScript")]
        public static bool GetScript(ExecutionEngine engine)
        {
            // Contract
            // returns byte[] 

            var context = engine.CurrentContext;
            var obj = context.EvaluationStack.Pop();
            var contract = ((VM.Types.InteropInterface)obj).GetInterface<Contract>();

            context.EvaluationStack.Push(contract.script);

            return true;
        }

        [Syscall("Neo.Runtime.Serialize")]
        public static bool Serialize(ExecutionEngine engine)
        {
            var context = engine.CurrentContext;
            using (MemoryStream ms = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(ms))
            {
                try
                {
                    SerializeStackItem(context.EvaluationStack.Pop(), writer);
                }
                catch (NotSupportedException)
                {
                    return false;
                }
                writer.Flush();
                if (ms.Length > engine.MaxItemSize)
                    return false;
                context.EvaluationStack.Push(ms.ToArray());
            }
            return true;
        }

        [Syscall("Neo.Runtime.Deserialize")]
        public static bool Deserialize(ExecutionEngine engine)
        {
            var context = engine.CurrentContext;
            byte[] data = context.EvaluationStack.Pop().GetByteArray();
            using (MemoryStream ms = new MemoryStream(data, false))
            using (BinaryReader reader = new BinaryReader(ms))
            {
                StackItem item;
                try
                {
                    item = DeserializeStackItem(reader,engine);
                }
                catch (FormatException)
                {
                    return false;
                }
                catch (IOException)
                {
                    return false;
                }
                context.EvaluationStack.Push(item);
            }
            return true;
        }


        [Syscall("Neo.Contract.GetStorageContext")]
        public static bool GetStorageContext(ExecutionEngine engine)
        {
            // Contract
            // returns StorageContext 
            throw new NotImplementedException();
        }

        [Syscall("Neo.Contract.Create", 500)]
        public static bool Create(ExecutionEngine engine)
        {
            var context = engine.CurrentContext;
            var script = context.EvaluationStack.Pop().GetByteArray();
            var parameterList = context.EvaluationStack.Pop().GetByteArray();
            var return_type = context.EvaluationStack.Pop();
            var need_storage = context.EvaluationStack.Pop().GetBoolean();
            var name = context.EvaluationStack.Pop().GetString();
            var version = context.EvaluationStack.Pop().GetString();
            var author = context.EvaluationStack.Pop().GetString();
            var email = context.EvaluationStack.Pop().GetString();
            var desc = context.EvaluationStack.Pop().GetString();

            //byte[] script, byte[] parameter_list, byte return_type, bool need_storage, string name, string version, string author, string email, string description

            var contract = new Contract();
            contract.script = script;

            var blockchain = engine.GetBlockchain();
            var account = blockchain.DeployContract(name, script);
            // TODO : merge Contract and Account

            context.EvaluationStack.Push(new VM.Types.InteropInterface<Contract>(contract));

            return true;
        }

        [Syscall("Neo.Contract.Migrate", 500)]
        public static bool Migrate(ExecutionEngine engine)
        {
            //byte[] script, byte[] parameter_list, byte return_type, bool need_storage, string name, string version, string author, string email, string description
            // returns Contract 
            throw new NotImplementedException();
        }

        [Syscall("Neo.Contract.Destroy")]
        public static bool Destroy(ExecutionEngine engine)
        {
            // returns nothing
            throw new NotImplementedException();
        }


        private static void SerializeStackItem(StackItem item, BinaryWriter writer)
        {
            List<StackItem> serialized = new List<StackItem>();
            Stack<StackItem> unserialized = new Stack<StackItem>();
            unserialized.Push(item);
            while (unserialized.Count > 0)
            {
                item = unserialized.Pop();
                switch (item)
                {
                    case ByteArray _:
                        writer.Write((byte)StackItemType.ByteArray);
                        writer.WriteVarBytes(item.GetByteArray());
                        break;
                    case VMBoolean _:
                        writer.Write((byte)StackItemType.Boolean);
                        writer.Write(item.GetBoolean());
                        break;
                    case Integer _:
                        writer.Write((byte)StackItemType.Integer);
                        writer.WriteVarBytes(item.GetByteArray());
                        break;
                    case InteropInterface _:
                        throw new NotSupportedException();
                    case VMArray array:
                        if (serialized.Any(p => ReferenceEquals(p, array)))
                            throw new NotSupportedException();
                        serialized.Add(array);
                        if (array is Struct)
                            writer.Write((byte)StackItemType.Struct);
                        else
                            writer.Write((byte)StackItemType.Array);
                        writer.WriteVarInt(array.Count);
                        for (int i = array.Count - 1; i >= 0; i--)
                            unserialized.Push(array[i]);
                        break;
                    case Map map:
                        if (serialized.Any(p => ReferenceEquals(p, map)))
                            throw new NotSupportedException();
                        serialized.Add(map);
                        writer.Write((byte)StackItemType.Map);
                        writer.WriteVarInt(map.Count);
                        foreach (var pair in map.Reverse())
                        {
                            unserialized.Push(pair.Value);
                            unserialized.Push(pair.Key);
                        }
                        break;
                }
            }
        }


        private static StackItem DeserializeStackItem(BinaryReader reader, ExecutionEngine engine)
        {
            Stack<StackItem> deserialized = new Stack<StackItem>();
            int undeserialized = 1;
            while (undeserialized-- > 0)
            {
                StackItemType type = (StackItemType)reader.ReadByte();
                switch (type)
                {
                    case StackItemType.ByteArray:
                        deserialized.Push(new ByteArray(reader.ReadVarBytes()));
                        break;
                    case StackItemType.Boolean:
                        deserialized.Push(new VMBoolean(reader.ReadBoolean()));
                        break;
                    case StackItemType.Integer:
                        deserialized.Push(new Integer(new BigInteger(reader.ReadVarBytes())));
                        break;
                    case StackItemType.Array:
                    case StackItemType.Struct:
                        {
                            int count = (int)reader.ReadVarInt(engine.MaxArraySize);
                            deserialized.Push(new ContainerPlaceholder
                            {
                                Type = type,
                                ElementCount = count
                            });
                            undeserialized += count;
                        }
                        break;
                    case StackItemType.Map:
                        {
                            int count = (int)reader.ReadVarInt(engine.MaxArraySize);
                            deserialized.Push(new ContainerPlaceholder
                            {
                                Type = type,
                                ElementCount = count
                            });
                            undeserialized += count * 2;
                        }
                        break;
                    default:
                        throw new FormatException();
                }
            }
            Stack<StackItem> stack_temp = new Stack<StackItem>();
            while (deserialized.Count > 0)
            {
                StackItem item = deserialized.Pop();
                if (item is ContainerPlaceholder placeholder)
                {
                    switch (placeholder.Type)
                    {
                        case StackItemType.Array:
                            VMArray array = new VMArray();
                            for (int i = 0; i < placeholder.ElementCount; i++)
                                array.Add(stack_temp.Pop());
                            item = array;
                            break;
                        case StackItemType.Struct:
                            Struct @struct = new Struct();
                            for (int i = 0; i < placeholder.ElementCount; i++)
                                @struct.Add(stack_temp.Pop());
                            item = @struct;
                            break;
                        case StackItemType.Map:
                            Map map = new Map();
                            for (int i = 0; i < placeholder.ElementCount; i++)
                            {
                                StackItem key = stack_temp.Pop();
                                StackItem value = stack_temp.Pop();
                                map.Add(key, value);
                            }
                            item = map;
                            break;
                    }
                }
                stack_temp.Push(item);
            }
            return stack_temp.Peek();
        }
    }



}
