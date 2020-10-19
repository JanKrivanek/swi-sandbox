using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Security.AccessControl;
using System.Threading;
using SolarWinds.SharedCommunication.Contracts.Utils;
using SolarWinds.SharedCommunication.Utils;

namespace SolarWinds.SharedCommunication.RateLimiter
{
    internal class RateLimiterSharedMemoryAccessor : IRateLimiterDataAccessor
    {
        private const long _INTERLOCK_LATCH_OFFSET = 0;

        //padding for rest of long
        private const long _CAPACITY_OFFSET = _INTERLOCK_LATCH_OFFSET + sizeof(long);
        private const long _SPAN_OFFSET = _CAPACITY_OFFSET + sizeof(long);
        private const long _SIZE_OFFSET = _SPAN_OFFSET + sizeof(long);
        private const long _CURRENT_IDX_OFFSET = _SIZE_OFFSET + sizeof(long);
        private const long _CONTENT_ADDRESS_OFFSET = _CURRENT_IDX_OFFSET + sizeof(long);

        //need to GC root this as view accessor doesn't take full ownership
        private readonly MemoryMappedFile _mmf;
        private readonly MemoryMappedViewAccessor _memoryAccessor;
        private readonly MemoryMappedViewAccessor _ringBuffermemoryAccessor;

        //we need the raw pointer for CAS operations
        private readonly IntPtr _latchAddress;

        public RateLimiterSharedMemoryAccessor(
            string segmentName, 
            int capacity, 
            long spanTicks, 
            IKernelObjectsPrivilegesChecker privilegesChecker)
        {
            //TODO: should acquire mutext (or better just the write latch)

            segmentName = privilegesChecker.KernelObjectsPrefix + segmentName;

            //this would be preventing code 
            var security = new MemoryMappedFileSecurity();
            security.AddAccessRule(new AccessRule<MemoryMappedFileRights>(
                "everyone",
                MemoryMappedFileRights.ReadWrite,
                AccessControlType.Allow));
            _mmf = MemoryMappedFile.CreateOrOpen(segmentName, _CONTENT_ADDRESS_OFFSET + capacity,
                MemoryMappedFileAccess.ReadWrite,
                MemoryMappedFileOptions.None, security, HandleInheritability.None);

            _memoryAccessor = _mmf.CreateViewAccessor(_INTERLOCK_LATCH_OFFSET, _CONTENT_ADDRESS_OFFSET);

            Capacity = (int)_memoryAccessor.ReadInt64(_CAPACITY_OFFSET);
            SpanTicks = _memoryAccessor.ReadInt64(_SPAN_OFFSET);

            if (Capacity == 0 && SpanTicks == 0)
            {
                _memoryAccessor.Write(_CAPACITY_OFFSET, (long)capacity);
                _memoryAccessor.Write(_SPAN_OFFSET, spanTicks);
                Capacity = capacity;
                SpanTicks = spanTicks;
            }
            else if (Capacity != capacity || SpanTicks != spanTicks)
            {
                throw new Exception(
                    $"Mismatch during RateLimiterSharedMemoryAccessor creation. [{segmentName}] had capacity set to {Capacity} and SpanTicks to {SpanTicks}, while caller requested {capacity}, {spanTicks}");
            }

            _ringBuffermemoryAccessor = _mmf.CreateViewAccessor(_CONTENT_ADDRESS_OFFSET, capacity * sizeof(long));

            //this points to start of MMF, not the view! (in our same luckily the same as offset is )
            _latchAddress = _memoryAccessor.SafeMemoryMappedViewHandle.DangerousGetHandle();
        }

        private const int _LOCK_TAKEN = 1;
        private const int _LOCK_FREE = 0;

        public unsafe bool TryEnterSynchronizedRegion()
        {
            return Interlocked.CompareExchange(ref (*((int*)(_latchAddress))), _LOCK_TAKEN, _LOCK_FREE) == _LOCK_FREE;
        }

        public void ExitSynchronizedRegion()
        {
            _memoryAccessor.Write(_INTERLOCK_LATCH_OFFSET, (int)0);
            //.net guarantees writes not to be reordered - but just for sure lets explicitly issue mem barrier
            Thread.MemoryBarrier();
        }

        public int Capacity { get; }
        public int Size
        {
            get => _memoryAccessor.ReadInt32(_SIZE_OFFSET);
            set => _memoryAccessor.Write(_SIZE_OFFSET, value >= Capacity ? Capacity : value);
        }

        private int GetWrapIndex(int i)
        {
            if (i < 0)
                return Capacity - 1;
            if (i >= Capacity)
                return 0;
            return i;
        }

        public long SpanTicks { get; }

        public long OldestTimestampTicks =>
            _ringBuffermemoryAccessor.ReadInt64(CurrentIndex * sizeof(long));

        private int CurrentIndex
        {
            get => _memoryAccessor.ReadInt32(_CURRENT_IDX_OFFSET);
            set => _memoryAccessor.Write(_CURRENT_IDX_OFFSET, GetWrapIndex(value));
        }

        public long CurrentTimestampTicks
        {
            get => _ringBuffermemoryAccessor.ReadInt64(GetWrapIndex(CurrentIndex - 1) * sizeof(long));

            set
            {
                _ringBuffermemoryAccessor.Write(CurrentIndex * sizeof(long), value);
                //properties handle wrapping appropriately
                Size++;
                CurrentIndex++;
            }
        }
    }
}