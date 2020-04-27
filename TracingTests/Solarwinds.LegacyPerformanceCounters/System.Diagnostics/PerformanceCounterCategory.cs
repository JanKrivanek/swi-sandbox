using System.Collections.Generic;
using System.Linq;

namespace System.Diagnostics
{
    /// <summary>
    /// Just a stub; All metrics just redirected to AppOptics
    /// see <see cref="PerformanceCounter"/> calls for actual redirection functionality
    /// </summary
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