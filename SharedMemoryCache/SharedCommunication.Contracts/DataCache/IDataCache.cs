using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SharedCommunication.Contracts.DataCache
{
    //TTL globally
    // Type constraint used to enforce type whitelist on deserialization of data coming from possible insecure surfaces (shared memory, remoting etc.)
    public interface IDataCache<T> where T: ICacheEntry
    {
        Task<T> GetData(Func<Task<T>> asyncDataFactory, CancellationToken token = default);
    }
}
