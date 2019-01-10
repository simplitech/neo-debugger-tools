using Neo.Lux.Core;
using Neo.VM;
using System;

namespace Neo.Emulation
{
    public class SmartContract
    {
        [OpCode(Lux.VM.OpCode.SHA1)]
        protected static byte[] Sha1(byte[] data)
        {
            throw new NotImplementedException();
        }

        [OpCode(Lux.VM.OpCode.SHA256)]
        protected static byte[] Sha256(byte[] data)
        {
            throw new NotImplementedException();
        }


        [OpCode(Lux.VM.OpCode.HASH160)]
        protected static byte[] Hash160(byte[] data)
        {
            throw new NotImplementedException();
        }


        [OpCode(Lux.VM.OpCode.HASH256)]
        protected static byte[] Hash256(byte[] data)
        {
            throw new NotImplementedException();
        }


        [OpCode(Lux.VM.OpCode.CHECKSIG)]
        protected static bool VerifySignature(byte[] signature, byte[] pubkey)
        {
            throw new NotImplementedException();
        }


        [OpCode(Lux.VM.OpCode.CHECKMULTISIG)]
        protected static bool VerifySignatures(byte[][] signature, byte[][] pubkey)
        {
            throw new NotImplementedException();
        }

    }
}
