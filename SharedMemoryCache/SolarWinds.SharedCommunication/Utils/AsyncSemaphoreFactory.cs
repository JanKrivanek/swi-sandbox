using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;
using SolarWinds.Coding.Utils.Logger;
using SolarWinds.SharedCommunication.Contracts.Utils;

namespace SolarWinds.SharedCommunication.Utils
{
    public class AsyncSemaphoreFactory : IAsyncSemaphoreFactory
    {
        private readonly ILogger _logger;

        public AsyncSemaphoreFactory(ILogger logger)
        {
            _logger = logger;
        }

        public IAsyncSemaphore Create(string name)
        {
            return this.Create(name, KernelObjectsPrivilegesChecker.GetInstance(_logger));
        }

        public IAsyncSemaphore Create(string name, IKernelObjectsPrivilegesChecker kernelObjectsPrivilegesChecker)
        {
            var allowEveryoneRule = new SemaphoreAccessRule(new SecurityIdentifier(WellKnownSidType.WorldSid, null),
                SemaphoreRights.FullControl, AccessControlType.Allow);
            SemaphoreSecurity securitySettings = new SemaphoreSecurity();
            securitySettings.AddAccessRule(allowEveryoneRule);

            name = kernelObjectsPrivilegesChecker.KernelObjectsPrefix + name;

            bool createdNew;
            Semaphore sp = new Semaphore(1, 1, name, out createdNew, securitySettings);

            string action = createdNew ? "Created new" : "Opened existing";
            _logger.Debug($"{action} Semaphore with name {name}.");

            return new AsyncSemaphore(sp);
        }
    }
}