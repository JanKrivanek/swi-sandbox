using System.Runtime.CompilerServices;
using System.Threading;

namespace System.Diagnostics
{
    public sealed class PerformanceCounter
    {
        public PerformanceCounter(string name = default, string description = default, PerformanceCounterType type = default)
        {
            CounterName = name;
            Description = description;
            Type = type;
        }

        private string _counterName;

        public string CounterName
        {
            get => _counterName;
            set
            {
                _counterName = value;
                RebuildAppOpticsIdentifier();
            }
        }

        private string _categoryName;
        public string CategoryName
        {
            get => _categoryName;
            set
            {
                _categoryName = value;
                RebuildAppOpticsIdentifier();
            }
        }
        public string Description { get; set; }
        public bool ReadOnly { get; set; }
        private string _instanceName;
        public string InstanceName
        {
            get => _instanceName;
            set
            {
                _instanceName = value;
                RebuildAppOpticsIdentifier();
            }
        }
        public PerformanceCounterInstanceLifetime InstanceLifetime { get; set; }
        public long RawValue
        {
            get => _rawValue;
            //No need to guard - 64bit type cannot have torn writes (.net have only aligned memory access)
            set
            {
                _rawValue = value;
                ReportMetric();
            }
        }
        private long _rawValue;
        public PerformanceCounterType Type { get; }

        private string _appOpticsIdentifier;
        private void RebuildAppOpticsIdentifier()
        {
            string id = $"{_categoryName}.{_categoryName}.{_instanceName}";
            id = id.Replace("..", ".");
            if (id.StartsWith("."))
            {
                id = id.Substring(1);
            }
            _appOpticsIdentifier = id;
        }

        public void RemoveInstance()
        {
            // noop
        }

        public void Increment()
        {
            Interlocked.Increment(ref _rawValue);
            ReportMetric();
        }

        public void IncrementBy(long by)
        {
            Interlocked.Add(ref _rawValue, by);
            ReportMetric();
        }

        public void Close()
        {
            // noop
        }

        public void Decrement()
        {
            Interlocked.Decrement(ref _rawValue);
            ReportMetric();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ReportMetric()
        {
            AppOptics.Instrumentation.Trace.SummaryMetric(_appOpticsIdentifier, _rawValue, 1, true);
        }

    }
}