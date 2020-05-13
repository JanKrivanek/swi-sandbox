using System;
using System.Collections.Generic;
using System.Text;

namespace SolarWinds.InMemoryCachingUtils
{
    public interface ITenantContext
    {
        string TenantIdentification { get; }
    }
}
