using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SharedCommunication.Contracts.DataCache
{
    //TTL globally
    public interface IDataCache<T>
    {
        Task<T> GetData(string key, Func<Task<T>> asyncDataFactory, CancellationToken token = default);
    }
}
