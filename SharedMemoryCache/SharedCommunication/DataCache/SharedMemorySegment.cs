using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security.AccessControl;
using System.Text;

namespace SharedCommunication.DataCache
{
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
            if (newSize > 0)
            {
                _contentMmf = MemoryMappedFile.CreateOrOpen(_contnetSegmentNamePreffix + newAddressGuid, newSize,
                    MemoryMappedFileAccess.ReadWrite,
                    MemoryMappedFileOptions.DelayAllocatePages, security, HandleInheritability.None);
                _contentMemoryStream = _contentMmf.CreateViewStream(0, newSize);
            }
            else
            {
                _contentMmf = null;
                _contentMemoryStream = null;
            }

            _lastKnownContentAddress = newAddressGuid;

            return _contentMemoryStream;
        }

        public T ReadData<T>()
        {
            var stream = EnsureContentStream();

            if (stream == null) return default;

            var ds = new DataContractSerializer(typeof(T));
            return (T)ds.ReadObject(stream);
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
            var stream = EnsureContentStream();
            if (stream == null) return new byte[0];

            byte[] data = new byte[ContentSize];
            MemoryStream ms = new MemoryStream(data);
            stream.CopyTo(ms);
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

        public void Clear()
        {
            ReserveMemorySegment(0);
            _memoryAccessor.Write(_SIZE_OFFSET, (long)0);
            _memoryAccessor.Write(_STAMP_OFFSET, (long)0);
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

        public void Dispose()
        {
            _mmf?.Dispose();
            _memoryAccessor?.Dispose();
            _contentMmf?.Dispose();
            _contentMemoryStream?.Dispose();
        }
    }
}
