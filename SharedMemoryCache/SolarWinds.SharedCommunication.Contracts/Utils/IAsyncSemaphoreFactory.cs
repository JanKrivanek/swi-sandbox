namespace SolarWinds.SharedCommunication.Contracts.Utils
{
    public interface IAsyncSemaphoreFactory
    {
        IAsyncSemaphore Create(string name);
        IAsyncSemaphore Create(string name, IKernelObjectsPrivilegesChecker kernelObjectsPrivilegesChecker);
    }
}