using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace System.Diagnostics
{
    /// <summary>
    /// Just redirects the metrics to AppOptics
    /// The metrics in AppOptics has the name in forma of <CategoryName>.<CounterName>.<InstanceName> (empty values and extra dots removed)
    /// Hostname tagging is on by default
    /// </summary>
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
            string id = $"{_categoryName}.{_counterName}.{_instanceName}";
            id = id.Replace("..", ".");
            if (id.StartsWith("."))
            {
                id = id.Substring(1);
            }
            _appOpticsIdentifier = id;
        }

        //TBD
        //public CounterSample NextSample()  { }

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
            //Carefull! We cannot use AppOptics.Instrumentation.Trace.Increment
            // despite having signed count argument the negative values underflows
            Interlocked.Decrement(ref _rawValue);
            ReportMetric();
        }

        private static readonly Dictionary<string, string> _processIdIdentification = new Dictionary<string, string>()
        {
            { 
                "PID",
                Process.GetCurrentProcess().Id.ToString()
            }
        };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ReportMetric()
        {
            AppOptics.Instrumentation.Trace.SummaryMetric(_appOpticsIdentifier, _rawValue, 1, true,
                //this is needed to be able to properly handle PerformanceCounterInstanceLifetime.Global vs
                //PerformanceCounterInstanceLifetime.Process
                _processIdIdentification);
        }

    }
}