using System.Collections.Generic;
using System.Linq;

namespace System.Diagnostics
{
    public class PerformanceCounterCategory
    {
        public static IEnumerable<PerformanceCounterCategory> GetCategories() =>
            Enumerable.Empty<PerformanceCounterCategory>();

        public string CategoryName { get; }
        public PerformanceCounterCategoryType CategoryType { get; }

        public static bool CounterExists(string counterName, string categoryName)
        {
            return false;
        }

        public static void Delete(string categoryName)
        {
            // noop
        }

        public static bool Exists(string categoryName)
        {
            return false;
        }

        public static void Create(string categoryName, string description, PerformanceCounterCategoryType categoryType, CounterCreationDataCollection perfCounterDefs)
        {
            // whatever, noop
        }
    }
}