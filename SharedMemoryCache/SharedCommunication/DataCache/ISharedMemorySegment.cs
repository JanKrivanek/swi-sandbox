using System;

namespace SharedCommunication.DataCache
{
    public interface ISharedMemorySegment
    {
        DateTime LastChangedUtc { get; }
        //long ContentSize { get; }
        //long Capacity { get; }
        T ReadData<T>();
        void WriteData<T>(T data);
        byte[] ReadBytes();
        void WriteBytes(byte[] bytes);
        void Clear();
    }
}