using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SolarWinds.SharedCommunication.Contracts.DataCache
{
    //TTL globally - or maybe per type, or maybe per call as in the WCF cache case
    // Type constraint used to enforce type whitelist on deserialization of data coming from possible insecure surfaces (shared memory, remoting etc.)
    public interface IDataCache<T>: IDisposable //where T: ICacheEntry
    {
        Task<T> GetData(Func<Task<T>> asyncDataFactory, CancellationToken token = default);
    }
}
