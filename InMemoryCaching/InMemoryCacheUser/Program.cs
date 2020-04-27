using System;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Caching.Redis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;
using SolarWinds.InMemoryCachingUtils;

namespace InMemoryCacheUser
{
    class Program
    {

        static void Main(string[] args)
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddDistributedRedisCache(options =>
            {
                options.Configuration = "sharedCache:6379";
                options.InstanceName = "redisInstance";
            });
            //OR
            //serviceCollection.AddDistributedMemoryCache();
            //OR
            //serviceCollection.AddMemoryCache()

            serviceCollection.AddLogging(opt =>
            {
                opt.AddConsole(copt => { copt.TimestampFormat = "yyyy-MM-dd HH:mm:ss.fffffff"; });
            });

            serviceCollection.AddScoped<IObjectCache<CacheEntry>, ObjectCacheFromDistributedWithFallbackToLocal<CacheEntry>>();
            serviceCollection.AddScoped<IRandomSamplesGenerator, RandomSamplesGenerator>();
            var container = serviceCollection.BuildServiceProvider();

            var serviceScopeFactory = container.GetRequiredService<IServiceScopeFactory>();
            using (var scope = serviceScopeFactory.CreateScope())
            {
                var runner = scope.ServiceProvider.GetService<IRandomSamplesGenerator>();
                runner.RunTest();
            }
        }
    }
}
