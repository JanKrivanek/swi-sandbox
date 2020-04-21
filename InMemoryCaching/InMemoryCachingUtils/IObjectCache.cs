using System;

namespace SolarWinds.InMemoryCachingUtils
{
    /// <summary>
    /// Taken from: https://bitbucket.solarwinds.com/projects/PAC/repos/interfaces/browse/Src/Web2/SolarWinds.Orion.Interfaces.WebApi/Caching/System/IObjectCache.cs#9
    /// 
    /// This interface describes a few methods from .NET Framework's ObjectCache class to let us easily mock it in unit tests.
    /// Objects in the cache can be of any type and are identified by string id.
    /// </summary>
    public interface IObjectCache<T> : IDisposable
    {
        /// <summary>
        /// Get object from cache.
        /// </summary>
        /// <returns>Returns cached objects or null if not present in cache.</returns>
        T Get(string key);

        /// <summary>
        /// Adds an object to the cache.
        /// </summary>
        void Add(string key, T item, DateTimeOffset absoluteExpiration);

        /// <summary>
        /// Removes an object from the cache.
        /// </summary>
        void Remove(string key);
    }
}