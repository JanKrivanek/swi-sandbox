using System.Diagnostics;
using SolarWinds.InMemoryCachingUtils;

namespace InMemoryCacheUser
{
    //Just a dummy illustrative implementation
    //Real implementation would fetch the context info from some implicit context (probably some AsyncLocal storage accessible via some static accessor)
    // - more details and prototype on that will be created separately
    public class TentantContext : ITenantContext
    {
        public string TenantIdentification { get; } = Process.GetCurrentProcess().Id.ToString();
    }
}