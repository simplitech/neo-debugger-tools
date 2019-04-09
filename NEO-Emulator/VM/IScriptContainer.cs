using Neo.VM;

namespace Neo_Emulator.VM
{
    public interface IScriptContainer : IInteropInterface
    {
        byte[] GetMessage();
    }
}
