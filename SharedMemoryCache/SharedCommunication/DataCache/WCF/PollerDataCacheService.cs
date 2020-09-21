using System.ServiceModel;
using SharedCommunication.Contracts.Utils;

namespace SharedCommunication.DataCache.WCF
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single,
        IncludeExceptionDetailInFaults = true,
        AutomaticSessionShutdown = true,
        ConcurrencyMode = ConcurrencyMode.Multiple)]
    public class PollerDataCacheService : PollerDataCacheImpl
    {
        private ServiceHost _service;

        public PollerDataCacheService(IDateTime dateTime) : base(dateTime)
        { }

        public void Start()
        {
            _service = new ServiceHost(this);
            _service.Open();
        }
    }
}