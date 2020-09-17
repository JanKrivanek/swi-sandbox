using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security.AccessControl;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SharedCommunication.RateLimiter;

namespace SahredMemoryUser
{
    //TODO - for synchronization: https://stackoverflow.com/a/63388583/2308106

    //todo: carefull about global namespace (MemoryMappedFile.CreateOrOpen will block if insufficient privileges :-/)

    public class MMFTests
    {
        public void Run()
        {
            byte b = 0;

            MMFWrapper m = new MMFWrapper("ghhgbaaah1");
            bool contine = true;
            while (contine)
            {
                m.currentCapacity = 100;
                m.CreateOrOpen();
                m.TestReadWrite(b);
                m.currentCapacity = 1000;
                m.CreateOrOpen();
                m.TestReadWrite(b);
                b++;
            }

            m.Dispose();
           //DataContractSerializer ds = new DataContractSerializer(typeof(string));
           //ds.WriteObject();
        }
    }

    public interface ISharedMemorySegment
    {
        DateTime LastChangedUtc { get; }
        //long ContentSize { get; }
        //long Capacity { get; }
        T ReadData<T>();
        void WriteData<T>(T data);
        byte[] ReadBytes();
        void WriteBytes(byte[] bytes);
    }

    public class SharedMemorySegment : ISharedMemorySegment
    {
        private const long _STAMP_OFFSET = 0;
        private const long _SIZE_OFFSET = _STAMP_OFFSET + sizeof(long);
        private const long _CAPACITY_OFFSET = _SIZE_OFFSET + sizeof(long);
        private const long _CONTENT_ADDRESS_OFFSET = _CAPACITY_OFFSET + sizeof(long);
        private static readonly long _HEADERS_SIZE = _CONTENT_ADDRESS_OFFSET + Marshal.SizeOf<Guid>(); //Guid doesn't have sizeof constant


        //need to GC root this as view accessor doesn't take full ownership
        private readonly MemoryMappedFile _mmf;
        private readonly MemoryMappedViewAccessor _memoryAccessor;
        //need to GC root this as stream accessor doesn't take full ownership
        private MemoryMappedFile _contentMmf;
        private MemoryMappedViewStream _contentMemoryStream;
        private Guid _lastKnownContentAddress = Guid.NewGuid();

        private readonly string _segmentName;
        private readonly string _contnetSegmentNamePreffix;

        public SharedMemorySegment(string segmentName)
        {
            //TODO: we should create it in global namespace; but for non-admin processes this
            // doesn't fail but rather blocks until somebody else creates the segment
            // So we need to check the privileges first
            _segmentName = /*@"Global\" +*/segmentName;
            _contnetSegmentNamePreffix = _segmentName + "_content_";

            var security = new MemoryMappedFileSecurity();
            security.AddAccessRule(new AccessRule<MemoryMappedFileRights>(
                "everyone",
                MemoryMappedFileRights.ReadWrite,
                AccessControlType.Allow));

            _mmf = MemoryMappedFile.CreateOrOpen(_segmentName, _HEADERS_SIZE, MemoryMappedFileAccess.ReadWrite,
                MemoryMappedFileOptions.DelayAllocatePages, security, HandleInheritability.None);
            _memoryAccessor = _mmf.CreateViewAccessor(0, _HEADERS_SIZE);
        }


        public DateTime LastChangedUtc => new DateTime(_memoryAccessor.ReadInt64(_STAMP_OFFSET), DateTimeKind.Utc);

        public long ContentSize => _memoryAccessor.ReadInt64(_SIZE_OFFSET);
        public long Capacity => _memoryAccessor.ReadInt64(_CAPACITY_OFFSET);

        public Guid ContentAddress
        {
            get
            {
                int sz = Marshal.SizeOf<Guid>(); //16
                byte[] guidData = new byte[sz];
                _memoryAccessor.ReadArray(_CONTENT_ADDRESS_OFFSET, guidData, 0, sz);
                return new Guid(guidData);
            }
        }

        private MemoryMappedViewStream EnsureContentStream()
        {
            if (_lastKnownContentAddress == ContentAddress)
            {
                return _contentMemoryStream;
            }

            //old MMF is obsolete - we need to release it
            _contentMemoryStream?.Dispose();
            _contentMmf?.Dispose();

            var security = new MemoryMappedFileSecurity();
            security.AddAccessRule(new AccessRule<MemoryMappedFileRights>(
                "everyone",
                MemoryMappedFileRights.ReadWrite,
                AccessControlType.Allow));

            Guid newAddressGuid = ContentAddress;
            long newSize = ContentSize;
            _contentMmf = MemoryMappedFile.CreateOrOpen(_contnetSegmentNamePreffix + newAddressGuid, newSize, MemoryMappedFileAccess.ReadWrite,
                MemoryMappedFileOptions.DelayAllocatePages, security, HandleInheritability.None);
            _contentMemoryStream = _contentMmf.CreateViewStream(0, newSize);
            _lastKnownContentAddress = newAddressGuid;

            return _contentMemoryStream;
        }

        public T ReadData<T>()
        {
            var ds = new DataContractSerializer(typeof(T));
            return (T)ds.ReadObject(EnsureContentStream());
        }

        public void WriteData<T>(T data)
        {
            var ds = new DataContractSerializer(typeof(T));
            MemoryStream ms = new MemoryStream();
            ds.WriteObject(ms, data);
            byte[] bytes = ms.ToArray();
            this.WriteBytes(bytes);
        }

        public byte[] ReadBytes()
        {
            byte[] data = new byte[ContentSize];
            MemoryStream ms = new MemoryStream(data);
            EnsureContentStream().CopyTo(ms);
            return data;
        }

        public void WriteBytes(byte[] bytes)
        {
            //if we have too small or too big segment
            if (bytes.Length > this.Capacity || GetPaddedSize(bytes.Length) < this.Capacity)
            {
                ReserveMemorySegment(GetPaddedSize(bytes.Length));
            }

            EnsureContentStream().Write(bytes, 0, bytes.Length);
            _memoryAccessor.Write(_SIZE_OFFSET, (long)bytes.Length);
            _memoryAccessor.Write(_STAMP_OFFSET, (long)DateTime.UtcNow.Ticks);
        }

        private void ReserveMemorySegment(int capacity)
        {
            _memoryAccessor.Write(_CAPACITY_OFFSET, (long)capacity);
            _memoryAccessor.WriteArray(_CONTENT_ADDRESS_OFFSET, Guid.NewGuid().ToByteArray(), 0,
                Marshal.SizeOf<Guid>());

            EnsureContentStream();
        }

        private static int CeilingToPowerOfTwo(int v)
        {
            //source: https://graphics.stanford.edu/~seander/bithacks.html#RoundUpPowerOf2
            v--;
            v |= v >> 1;
            v |= v >> 2;
            v |= v >> 4;
            v |= v >> 8;
            v |= v >> 16;
            v++;
            return v;
        }

        private static int GetPaddedSize(int size)
        {
            const int megabyte = 1024 * 1024; //1 MB
            const int doublingThreshold = 20 * megabyte; //20 MBs

            return size < doublingThreshold ? CeilingToPowerOfTwo(size) : (size + megabyte);
        }
    }

    public class SynchronizedSharedMemorySegment : ISharedMemorySegment
    {
        private readonly ISharedMemorySegment _memorySegment;
        private readonly IMutex _mutex;

        public SynchronizedSharedMemorySegment(ISharedMemorySegment memorySegment, IMutex mutex)
        {
            _memorySegment = memorySegment;
            _mutex = mutex;
        }

        public DateTime LastChangedUtc => _memorySegment.LastChangedUtc;

        private T ExecuteLocked<T>(Func<T> func)
        {
            using (_mutex.Lock())
            {
                return func();
            }
        }

        private void ExecuteLocked(Action action)
        {
            ExecuteLocked(() =>
            {
                action();
                return true;
            });
        }

        public T ReadData<T>()
        {
            return ExecuteLocked(_memorySegment.ReadData<T>);
        }

        public void WriteData<T>(T data)
        {
            ExecuteLocked(() => _memorySegment.WriteData<T>(data));
        }

        public byte[] ReadBytes()
        {
            return ExecuteLocked(_memorySegment.ReadBytes);
        }

        public void WriteBytes(byte[] bytes)
        {
            ExecuteLocked(() => _memorySegment.WriteBytes(bytes));
        }
    }

    public interface IDataCache<T>
    {
        Task<T> GetData(Func<Task<T>> asyncDataFactory, CancellationToken token = default);
    }

    public class DataCacheSettings
    {
        public TimeSpan Ttl { get; set; }
        public string CacheName { get; set; }
    }

    public class AsyncSemaphore
    {
        private readonly Semaphore _sp;

        public AsyncSemaphore(string name)
        {
            bool createdNew;
            _sp = new Semaphore(1, 1, name, out createdNew);

        }

        private class WaitInfo
        {
            public IDisposable Handle { get; set; }
        }

        public Task WaitAsync(CancellationToken token = default)
        {
            TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();
            if (token.IsCancellationRequested)
            {
                tcs.TrySetCanceled();
                return tcs.Task;
            }
            WaitInfo waitInfo = new WaitInfo();

            var reg = ThreadPool.RegisterWaitForSingleObject(_sp, (state, timedout) =>
            {
                tcs.TrySetResult(true);
                waitInfo.Handle?.Dispose();
            }, null, Timeout.InfiniteTimeSpan, true);
            //here is a small space for race condition - token getting cancelled right after the task getting registered and finished
            // and before registering the cancellation token. In such a case, the token would remain registered and upon cancellation (if any)
            // the unregistration of wait handle from TP would not succeed anyway - as TP registration is no more valid
            waitInfo.Handle = token.Register(() =>
            {
                reg.Unregister(null);
                tcs.TrySetCanceled();
            });
            return tcs.Task;
        }

        public void Release()
        {
            _sp.Release();
        }
    }

    public class DataCache<T> : IDataCache<T>
    {
        //MMF and mutex can be created new with same name and would correctly use same resources
        // however the SemaphoreSlim cannot be created from handle - so we need to make sure to create single
        private static ConcurrentDictionary<string, IDataCache<T>> _instances = new ConcurrentDictionary<string, IDataCache<T>>();
        private readonly SemaphoreSlim _semaphoreSlim;
        private readonly ISharedMemorySegment _memorySegment;
        private readonly TimeSpan _ttl;
        private readonly IDateTime _dateTime;

        public static IDataCache<T> Create(string cacheName, TimeSpan ttl, IDateTime dateTime,
            IMutexFactory mutexFactory)
        {
            return _instances.GetOrAdd(cacheName, name => new DataCache<T>(name, ttl, dateTime, mutexFactory));
        }

        public static IDataCache<T> Create(DataCacheSettings settings, IDateTime dateTime, IMutexFactory mutexFactory)
        {
            return Create(settings.CacheName, settings.Ttl, dateTime, mutexFactory);
        }

        private DataCache(string cacheName, TimeSpan ttl, IDateTime dateTime, IMutexFactory mutexFactory)
        {
            //TODO: to be added to run properly accross sessions
            //cacheName = @"Global\" + cacheName;

            _semaphoreSlim = new SemaphoreSlim(1,1);
            IMutex mutex = mutexFactory.Create(cacheName + "_MTX");
            _memorySegment = new SynchronizedSharedMemorySegment(new SharedMemorySegment(cacheName + "_MMF"), mutex);
            _ttl = ttl;
            _dateTime = dateTime;
        }

        public async Task<T> GetData(Func<Task<T>> asyncDataFactory, CancellationToken token = default)
        {
            await _semaphoreSlim.WaitAsync(token);
            //if token cancelled OperationCanceledException is thrown - so we won't get here
            try
            {
                bool hasData = _memorySegment.LastChangedUtc >= _dateTime.UtcNow - _ttl;
                T data;
                if (hasData)
                {
                    data = _memorySegment.ReadData<T>();
                }
                else
                {
                    //this is locked only locally (process), not globally (system) - so this implementation leaves space for
                    // unnecessary multiple parallel (by multiple processes) creation of data
                    data = await asyncDataFactory();
                    _memorySegment.WriteData(data);
                }

                return data;
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }


        public async Task<T> GetData2(Func<Task<T>> asyncDataFactory, CancellationToken token = default)
        {
            


            //this would be private field of class
            IMutex mutex = null;

            await _semaphoreSlim.WaitAsync(token);
            IDisposable lockContext = null;
            //if token cancelled OperationCanceledException is thrown - so we won't get here
            try
            {
                //even though we acquire fat lock here for duration of creation of data - we still guard by local
                // sempahore slim, so majority of time we will 'wait' asynchronously (provided that the caller within
                // critical section is from same process)
                lockContext = mutex.Lock();
                bool hasData = _memorySegment.LastChangedUtc >= _dateTime.UtcNow - _ttl;
                T data;
                if (hasData)
                {
                    data = _memorySegment.ReadData<T>();
                }
                else
                {
                    //this is locked only locally (process), not globally (system) - so this implementation leaves space for
                    // unnecessary multiple parallel (by multiple processes) creation of data
                    data = await asyncDataFactory();
                    _memorySegment.WriteData(data);
                }

                return data;
            }
            finally
            {
                lockContext?.Dispose();
                _semaphoreSlim.Release();
            }
        }
    }
}
