namespace System.Diagnostics
{
    /// <summary>
    /// Just a stub; All metrics just redirected to AppOptics
    /// see <see cref="PerformanceCounter"/> calls for actual redirection functionality
    /// </summary>
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