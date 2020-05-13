﻿using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
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

            //serviceCollection.AddDistributedRedisCache(options =>
            //{
            //    options.Configuration = "sharedCache:6379";
            //    options.InstanceName = "redisInstance";
            //});
            //OR
            //serviceCollection.AddDistributedMemoryCache();

            //MS DI doesn't support decorator pattern :-/
            // So we need to do this super ugly way :-// (concrete types, new in injection etc.)
            //To solve we can e.g. use Scrutor and then simply
            //serviceCollection.Decorate<IDistributedCache, MultiTenantDistributedCache>
            serviceCollection.AddOptions();
            serviceCollection.Configure((Action<RedisCacheOptions>) (options =>
            {
                options.Configuration = "sharedCache:6379";
                options.InstanceName = "redisInstance";
            }));
            serviceCollection.AddScoped<RedisCache>();

            //serviceCollection.AddOptions();
            //serviceCollection.AddScoped<MemoryDistributedCache>();

            //OR
            //serviceCollection.AddMemoryCache()

            serviceCollection.AddLogging(opt =>
            {
                opt.AddConsole(copt => { copt.TimestampFormat = "yyyy-MM-dd HH:mm:ss.fffffff"; });
            });


            serviceCollection.AddScoped<ITenantContext, TentantContext>();

            serviceCollection.AddScoped<IDistributedCache>(provider =>
                new MultiTenantDistributedCache(provider.GetRequiredService<ITenantContext>(),
                    provider.GetRequiredService<RedisCache>()));

            serviceCollection.AddScoped<IObjectCache<CacheEntry>, ObjectCacheFromDistributedWithFallbackToLocal<CacheEntry>>();
            serviceCollection.AddScoped<IRandomSamplesGenerator, RandomSamplesGenerator>();
            serviceCollection
                .AddSingleton<ISerializer<CacheEntry>, SWDataContractSerializer<CacheEntry>>();
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
