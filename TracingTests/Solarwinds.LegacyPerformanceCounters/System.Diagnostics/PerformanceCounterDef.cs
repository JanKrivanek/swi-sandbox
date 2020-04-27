namespace System.Diagnostics
{
    /// <summary>
    /// Just a stub; All metrics just redirected to AppOptics
    /// see <see cref="PerformanceCounter"/> calls for actual redirection functionality
    /// </summary
    public sealed class PerformanceCounterDef
    {
        public PerformanceCounterDef(string name, string description, PerformanceCounterType type)
        {
            Name = name;
            Description = description;
            Type = type;
        }

        public string Name { get; }
        public string Description { get; }
        public PerformanceCounterType Type { get; }
    }
}