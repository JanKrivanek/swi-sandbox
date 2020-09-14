using System;
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
        long ContentSize { get; }
        long Capacity { get; }
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


    public class SharedMemorySegmentV1 : ISharedMemorySegment
    {
        private const long _STAMP_OFFSET = 0;
        private const long _SIZE_OFFSET = _STAMP_OFFSET + sizeof(long);
        private const long _CAPACITY_OFFSET = _SIZE_OFFSET + sizeof(long);
        private const long _CONTENT_OFFSET = _CAPACITY_OFFSET + sizeof(long);


        //need to GC root this as view accessor doesn't take full ownership
        private MemoryMappedFile _mmf;
        private MemoryMappedViewAccessor _memoryAccessor;
        private MemoryMappedViewStream _memoryStream;

        private readonly string _segmentName;

        public SharedMemorySegmentV1(string segmentName)
        {
            //TODO: we should create it in global namespace; but for non-admin processes this
            // doesn't fail but rather blocks until somebody else creates the segment
            // So we need to check the privileges first
            _segmentName = /*@"Global\" +*/segmentName;
        }


        public DateTime LastChangedUtc => new DateTime(_memoryAccessor.ReadInt64(_STAMP_OFFSET), DateTimeKind.Utc);

        public long ContentSize => _memoryAccessor.ReadInt64(_SIZE_OFFSET);
        public long Capacity => _memoryAccessor.ReadInt64(_CAPACITY_OFFSET);

        public T ReadData<T>()
        {
            var ds = new DataContractSerializer(typeof(T));
            return (T) ds.ReadObject(_memoryStream);
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
            _memoryStream.CopyTo(ms);
            return data;
        }

        public void WriteBytes(byte[] bytes)
        {
            //if we have too small or too big segment
            if (bytes.Length > this.Capacity || GetPaddedSize(bytes.Length) < this.Capacity)
            {
                ReserveMemorySegment(GetPaddedSize(bytes.Length));
            }

            _memoryStream.Write(bytes, 0, bytes.Length);
            _memoryAccessor.Write(_SIZE_OFFSET, (long)bytes.Length);
            _memoryAccessor.Write(_STAMP_OFFSET, (long)DateTime.UtcNow.Ticks);
        }

        private void ReserveMemorySegment(int capacity)
        {
            _mmf?.Dispose();
            _memoryAccessor?.Dispose();
            _memoryStream?.Dispose();

            var security = new MemoryMappedFileSecurity();
            security.AddAccessRule(new AccessRule<MemoryMappedFileRights>(
                "everyone",
                MemoryMappedFileRights.ReadWrite,
                AccessControlType.Allow));

            _mmf = MemoryMappedFile.CreateOrOpen(_segmentName, capacity + _CONTENT_OFFSET, MemoryMappedFileAccess.ReadWrite,
                MemoryMappedFileOptions.DelayAllocatePages, HandleInheritability.None);
            _memoryAccessor = _mmf.CreateViewAccessor(0, _CONTENT_OFFSET);
            _memoryStream = _mmf.CreateViewStream(_CONTENT_OFFSET, capacity);

            _memoryAccessor.Write(_CAPACITY_OFFSET, (long)capacity);
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

            int paddedSize = size < doublingThreshold ? CeilingToPowerOfTwo(size) : (size + megabyte);

            //we are creating segments with metadata - so want to align padding accordingly
            paddedSize -= (int)_CONTENT_OFFSET;

            return Math.Max(size, paddedSize);
        }
    }

    public class MMFWrapper
    {
        //private ToBSenderUnit[] _senderUnits = new ToBSenderUnit[Symbol.ValuesCount];
        private MemoryMappedViewAccessor _memoryAccessor;
        //need to GC root this as view accessor doesn't take full ownership
        private MemoryMappedFile _mmf;
        public long currentCapacity;
        private EventWaitHandle _sharedSignalEvent;

        private readonly string _mmfName;

        public MMFWrapper(string name)
        {
            _mmfName = /*@"Global\" +*/ name;

        }

        public void TestReadWrite(byte b)
        {
            byte was = _memoryAccessor.ReadByte(currentCapacity - 1);
            _memoryAccessor.Write(currentCapacity - 1, (byte)b);
        }

        public void Dispose()
        {
            _mmf?.Dispose();
            _memoryAccessor?.Dispose();
        }

        public void CreateOrOpen()
        {
            _mmf?.Dispose();
            _memoryAccessor?.Dispose();

            var security = new MemoryMappedFileSecurity();
            security.AddAccessRule(new AccessRule<MemoryMappedFileRights>(
                "everyone",
                MemoryMappedFileRights.ReadWrite,
                AccessControlType.Allow));

            _mmf = MemoryMappedFile.CreateOrOpen(_mmfName, currentCapacity, MemoryMappedFileAccess.ReadWrite,
                MemoryMappedFileOptions.DelayAllocatePages, HandleInheritability.None);
            long hdl = _mmf.SafeMemoryMappedFileHandle.DangerousGetHandle().ToInt64();
            TrashMemory();
            _memoryAccessor = _mmf.CreateViewAccessor(0, currentCapacity, MemoryMappedFileAccess.ReadWrite);
        }

        private void TrashMemory()
        {
            for (int i = 0; i < 10; i++)
            {
                int sz = 65000000;
                var mmf = MemoryMappedFile.CreateOrOpen("aaa" + i, sz, MemoryMappedFileAccess.ReadWrite,
                    MemoryMappedFileOptions.DelayAllocatePages, HandleInheritability.None);
                var view = mmf.CreateViewAccessor(0,sz);
                view.Write(sz-1, (byte)5);
            }
        }
    }
}
