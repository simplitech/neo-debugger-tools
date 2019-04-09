using Neo.Lux.Utils;

namespace Neo.VM
{
    public class Crypto : ICrypto
    {
        public byte[] Hash160(byte[] message)
        {
            return CryptoUtils.Hash160(message);
        }

        public byte[] Hash256(byte[] message)
        {
            return CryptoUtils.Hash256(message);
        }

        public bool VerifySignature(byte[] message, byte[] signature, byte[] pubkey)
        {
            return CryptoUtils.VerifySignature(message, signature, pubkey);
        }
    }
}
