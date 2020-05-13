using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;

namespace SolarWinds.InMemoryCachingUtils
{
    public class MultiTenantDistributedCache: IDistributedCache
    {
        private readonly string _tenantId;
        private readonly IDistributedCache _cache;

        public MultiTenantDistributedCache(ITenantContext tenantContext, IDistributedCache cache)
        {
            _tenantId = tenantContext.TenantIdentification + ".";
            _cache = cache;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private string GetTenantKey(string key)
        {
            return _tenantId + key;
        }

        public byte[] Get(string key)
        {
            return _cache.Get(GetTenantKey(key));
        }

        public Task<byte[]> GetAsync(string key, CancellationToken token = new CancellationToken())
        {
            return _cache.GetAsync(GetTenantKey(key), token);
        }

        public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
        {
            _cache.Set(GetTenantKey(key), value, options);
        }

        public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options,
            CancellationToken token = new CancellationToken())
        {
            return _cache.SetAsync(GetTenantKey(key), value, options, token);
        }

        public void Refresh(string key)
        {
            _cache.Refresh(GetTenantKey(key));
        }

        public Task RefreshAsync(string key, CancellationToken token = new CancellationToken())
        {
            return _cache.RefreshAsync(GetTenantKey(key), token);
        }

        public void Remove(string key)
        {
            _cache.Remove(GetTenantKey(key));
        }

        public Task RemoveAsync(string key, CancellationToken token = new CancellationToken())
        {
            return _cache.RemoveAsync(GetTenantKey(key), token);
        }
    }
}
