using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SahredMemoryUser
{

    public class TestRate
    {
        public void RunTest()
        {
            RateLimiterSharedMemoryAccessor ma = new RateLimiterSharedMemoryAccessor("fdffd", 4, 20);

            bool enter1 = ma.TryEnterSynchronizedRegion();
            bool enter2 = ma.TryEnterSynchronizedRegion();

            for (int i = 10; i < 20; i++)
            {
                long o = ma.OldestTimestampTicks;
                long c = ma.CurrentTimestampTicks;
                ma.CurrentTimestampTicks = i;
            }

            ma.ExitSynchronizedRegion();

            bool enter3 = ma.TryEnterSynchronizedRegion();
            bool enter4 = ma.TryEnterSynchronizedRegion();
        }
    }

    public interface ICrossProcessRateLimiterFactory
    {
        IRateLimiter OpenOrCreate(string identifier, TimeSpan measureTime, int maxOccurencesPerTime);
        IRateLimiter OpenExisting(string identifier);
    }

    public interface IRateLimiter
    {
        Task<bool> WaitTillNextFreeSlotAsync(TimeSpan maxAcceptableWaitingTime, CancellationToken cancellationToken = default);
        bool BlockTillNextFreeSlot(TimeSpan maxAcceptableWaitingTime);

    }

    public interface IRateLimiterDataAccessor
    {
        bool TryEnterSynchronizedRegion();
        void ExitSynchronizedRegion();
        int Capacity { get; }
        int Size { get; }
        long SpanTicks { get; }
        long OldestTimestampTicks { get; }
        long CurrentTimestampTicks { get; set; }

    }

    public class RateLimiterSharedMemoryAccessor : IRateLimiterDataAccessor
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

        public RateLimiterSharedMemoryAccessor(string segmentName, int capacity, long spanTicks)
        {
            //TODO: should acquire mutext (or better just the write latch)

            //segmentName = @"Global\" + segmentName;

            var security = new MemoryMappedFileSecurity();
            security.AddAccessRule(new AccessRule<MemoryMappedFileRights>(
                "everyone",
                MemoryMappedFileRights.ReadWrite,
                AccessControlType.Allow));
            _mmf = MemoryMappedFile.CreateOrOpen(segmentName, _CONTENT_ADDRESS_OFFSET + capacity,
                MemoryMappedFileAccess.ReadWrite,
                MemoryMappedFileOptions.None, security, HandleInheritability.None);

            _memoryAccessor = _mmf.CreateViewAccessor(_INTERLOCK_LATCH_OFFSET, _CONTENT_ADDRESS_OFFSET);

            Capacity = (int) _memoryAccessor.ReadInt64(_CAPACITY_OFFSET);
            SpanTicks = _memoryAccessor.ReadInt64(_SPAN_OFFSET);

            if (Capacity == 0 && SpanTicks == 0)
            {
                _memoryAccessor.Write(_CAPACITY_OFFSET, (long) capacity);
                _memoryAccessor.Write(_SPAN_OFFSET, spanTicks);
                Capacity = capacity;
                SpanTicks = spanTicks;
            }
            else if(Capacity != capacity || SpanTicks != spanTicks)
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
            return Interlocked.CompareExchange(ref (*((int*) (_latchAddress))), _LOCK_TAKEN, _LOCK_FREE) == _LOCK_FREE;
        }

        public void ExitSynchronizedRegion()
        {
            _memoryAccessor.Write(_INTERLOCK_LATCH_OFFSET, (int) 0);
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
                _ringBuffermemoryAccessor.Write(CurrentIndex*sizeof(long), value);
                //properties handle wrapping appropriately
                Size++;
                CurrentIndex++;
            }
        }
    }

    public interface IDateTime
    {
        DateTime UtcNow { get; }
    }

    public class PlatformDateTime : IDateTime
    {
        public DateTime UtcNow => DateTime.UtcNow;
    }

    public class RingMemoryBufferRateLimiter : IRateLimiter
    {
        private readonly IRateLimiterDataAccessor _rateLimiterDataAccessor;
        private readonly IDateTime _dateTime;
        private readonly int _rateLimiterCapacity;
        private readonly TimeSpan _rateLimiterSpan;

        public RingMemoryBufferRateLimiter(
            IRateLimiterDataAccessor rateLimiterDataAccessor,
            IDateTime dateTime)
        {
            _rateLimiterDataAccessor = rateLimiterDataAccessor;
            _dateTime = dateTime;

            _rateLimiterCapacity = _rateLimiterDataAccessor.Capacity;
            _rateLimiterSpan = new TimeSpan(_rateLimiterDataAccessor.SpanTicks);
        }

        private void EnterSynchronization()
        {
            SpinWait.SpinUntil(_rateLimiterDataAccessor.TryEnterSynchronizedRegion);
        }

        //Depending on version of OS and .NET framework, the granularity of timer and timer events can 1-15ms (15ms being the usual)
        // this could lead to 'false wake-up' issues during contention (leading to resonated contention)
        private TimeSpan GetRandomSaltSpan()
        {
            return TimeSpan.FromMilliseconds(new Random().Next(20));
        }

        private bool ClaimSlotAndGetWaitingTime(TimeSpan maxAcceptableWaitingTime, out TimeSpan waitSpan)
        {
            DateTime utcNow;
            waitSpan = TimeSpan.Zero;
            bool isAcceptable = true;
            try
            {
                EnterSynchronization();
                utcNow = _dateTime.UtcNow;
                bool isFull = _rateLimiterDataAccessor.Size == _rateLimiterCapacity;
                
                if (isFull)
                {
                    DateTime oldestEventUtc =
                        new DateTime(_rateLimiterDataAccessor.OldestTimestampTicks);
                    waitSpan = _rateLimiterSpan - (utcNow - oldestEventUtc);
                    //prevent false wake ups by randomness
                    waitSpan = waitSpan <= TimeSpan.Zero ? TimeSpan.Zero : (waitSpan + GetRandomSaltSpan());
                }

                isAcceptable = waitSpan <= maxAcceptableWaitingTime;
                if (isAcceptable)
                {
                    _rateLimiterDataAccessor.CurrentTimestampTicks = (utcNow + waitSpan).Ticks;
                }
            }
            finally
            {
                _rateLimiterDataAccessor.ExitSynchronizedRegion();
            }

            return isAcceptable;
        }

        private static readonly Task<bool> _success = Task.FromResult(true);
        private static readonly Task<bool> _failure = Task.FromResult(false);
        public Task<bool> WaitTillNextFreeSlotAsync(TimeSpan maxAcceptableWaitingTime, CancellationToken cancellationToken = default)
        {
            TimeSpan waitSpan;
            if (!ClaimSlotAndGetWaitingTime(maxAcceptableWaitingTime, out waitSpan))
            {
                return _failure;
            }

            if (waitSpan <= TimeSpan.Zero)
                return _success;
            else
                return Task.Delay(waitSpan, cancellationToken).ContinueWith(t => !t.IsCanceled);
        }

        public bool BlockTillNextFreeSlot(TimeSpan maxAcceptableWaitingTime)
        {
            TimeSpan waitSpan;
            if (!ClaimSlotAndGetWaitingTime(maxAcceptableWaitingTime, out waitSpan))
            {
                return false;
            }

            if (waitSpan > TimeSpan.Zero)
                Thread.Sleep(waitSpan);

            return true;
        }
    }
}

