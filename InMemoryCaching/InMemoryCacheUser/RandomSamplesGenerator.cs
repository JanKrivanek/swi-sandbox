using System;
using System.Diagnostics;
using System.Net;
using System.Runtime.Serialization;
using SolarWinds.InMemoryCachingUtils;

namespace InMemoryCacheUser
{
    [DataContract]
    public class CacheEntry
    {
        [DataMember]
        public string Value { get; set; }
    }

    public interface IRandomSamplesGenerator
    {
        void RunTest();
    }

    public class RandomSamplesGenerator : IRandomSamplesGenerator
    {
        private readonly IObjectCache<CacheEntry> _cache;

        private static readonly string _prefix = Dns.GetHostName() + "-" +
                                                 Process.GetCurrentProcess().Id;

        public RandomSamplesGenerator(IObjectCache<CacheEntry> cache)
        {
            _cache = cache;
        }

        public void RunTest()
        {
            while (true)
            {
                AddSingleSample();
                System.Threading.Thread.Sleep(900);
            }

        }

        private readonly Random _rnd = new Random();
        private readonly TimeSpan _expiration = TimeSpan.FromSeconds(5);
        private void AddSingleSample()
        {
            string key = _rnd.Next(10).ToString();
            CacheEntry entry = _cache.Get(key);

            if (entry == null)
            {
                string value =
                    $"{_prefix}-{DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fffffff")}";
                entry = new CacheEntry(){Value = value};

                Console.WriteLine($"{key} - NOT found in cache. Value: {entry.Value}");

                _cache.Add(key, entry, new DateTimeOffset(DateTime.UtcNow.Add(_expiration)));
            }
            else
            {
                Console.WriteLine($"{key} - found in cache. Value: {entry.Value}");
            }
        }
    }
}
