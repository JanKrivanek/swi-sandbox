using System.Collections.Generic;

namespace System.Diagnostics
{
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