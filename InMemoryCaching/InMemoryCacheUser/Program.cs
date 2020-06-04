using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Security.Authentication;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Caching.Redis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;
using SolarWinds.InMemoryCachingUtils;
using StackExchange.Redis;

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
                //options.Configuration = "sharedCache:6379";
                //options.InstanceName = "redisInstance";
                options.ConfigurationOptions = new ConfigurationOptions()
                {
                    //TODO: those should go through IConfiguration
                    EndPoints = { "sharedCache:6379" },
                    Password = Environment.GetEnvironmentVariable("REDIS_PASSWORD"),
                    //For this we'd need to enable the ssl in redis.conf http://download.redis.io/redis-stable/redis.conf
                    // then we can either build the new image with that: https://hub.docker.com/_/redis/
                    // or inject it via shared volumes: http://kb.objectrocket.com/redis/run-redis-with-docker-compose-1055#create+a+docker-compose+file+for+redis
                    //HOWEVER - we won't very likely use the redis container, but aws ElastiCache - the encryption (in-transit and at-rest) should
                    // be configured directly there (and then either terminated in side car, or in the code - that needs to be investigated):
                    //   https://aws.amazon.com/about-aws/whats-new/2017/10/amazon-elasticache-for-redis-now-supports-in-transit-and-at-rest-encryption-to-help-protect-sensitive-information/
                    //Ssl = true,
                    //SslProtocols = SslProtocols.Tls

                };
            }));
            serviceCollection.AddScoped<RedisCache>();

            //serviceCollection.AddOptions();
            //serviceCollection.AddScoped<MemoryDistributedCache>();

            //OR
            //serviceCollection.AddMemoryCache()

            serviceCollection.AddLogging(opt =>
            {
                opt.AddConsole(copt => { copt.TimestampFormat = "yyyy-MM-dd HH:mm:ss.fffffff"; });
                opt.SetMinimumLevel(LogLevel.Trace);
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
