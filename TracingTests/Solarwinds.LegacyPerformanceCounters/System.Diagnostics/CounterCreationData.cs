namespace System.Diagnostics
{
    public sealed class CounterCreationData
    {
        public CounterCreationData(string counterName, string description, PerformanceCounterType counterType)
        {
            CounterName = counterName;
            Description = description;
            CounterType = counterType;
        }

        public string CounterName { get; }
        public string Description { get; }
        public PerformanceCounterType CounterType { get; }
    }
}