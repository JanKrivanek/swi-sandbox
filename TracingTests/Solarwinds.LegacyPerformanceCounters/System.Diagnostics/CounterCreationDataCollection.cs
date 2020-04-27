using System.Collections.Generic;

namespace System.Diagnostics
{
    /// <summary>
    /// Just a stub; All metrics just redirected to AppOptics
    /// see <see cref="PerformanceCounter"/> calls for actual redirection functionality
    /// </summary>
    public sealed class CounterCreationDataCollection
    {
        private readonly List<CounterCreationData> _data = new List<CounterCreationData>();
        public int Count => _data.Count;

        public void Add(CounterCreationData ccd)
        {
            _data.Add(ccd);
        }

        public List<CounterCreationData>.Enumerator GetEnumerator() => _data.GetEnumerator();

        public CounterCreationData this[int i] => _data[i];
    }
}