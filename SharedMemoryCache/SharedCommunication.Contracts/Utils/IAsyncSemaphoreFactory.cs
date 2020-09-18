namespace SharedCommunication.Contracts.Utils
{
    public interface IAsyncSemaphoreFactory
    {
        IAsyncSemaphore Create(string name);
    }
}