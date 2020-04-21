namespace System.Diagnostics
{
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