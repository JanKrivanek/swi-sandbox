using System;
using System.Collections.Generic;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SolarWinds.Coding.Utils.Logger;
using SolarWinds.SharedCommunication.Contracts.Utils;

namespace SolarWinds.SharedCommunication.Utils
{
    public class KernelObjectsPrivilegesChecker : IKernelObjectsPrivilegesChecker
    {
        public bool CanWriteToGlobalNamespace => _instance.CanWriteToGlobalNamespace;
        public string KernelObjectsPrefix => _instance.KernelObjectsPrefix;
        private static IKernelObjectsPrivilegesChecker _instance;

        public static IKernelObjectsPrivilegesChecker GetInstance(ILogger logger)
        {
            //no synchro needed here; we're fine with race
            if (_instance == null)
            {
                _instance = new KernelObjectsPrivilegesCheckerImpl(logger);
            }

            return _instance;
        }

        public KernelObjectsPrivilegesChecker(ILogger logger)
        {
            GetInstance(logger);
        }

        private class KernelObjectsPrivilegesCheckerImpl : IKernelObjectsPrivilegesChecker
        {
            public bool CanWriteToGlobalNamespace { get; }
            public string KernelObjectsPrefix { get; }

            public KernelObjectsPrivilegesCheckerImpl(ILogger logger)
            {
                CanWriteToGlobalNamespace = CanCreateMmfInGlobalNamespace(logger);
                KernelObjectsPrefix =
                    CanWriteToGlobalNamespace ? _GLOBAL_NAMESPACE_PREFIX : string.Empty;
            }

            private const string _GLOBAL_NAMESPACE_PREFIX = "Global\\";

            private bool CanCreateMmfInGlobalNamespace(ILogger logger)
            {
                try
                {
                    var f = MemoryMappedFile.CreateNew(_GLOBAL_NAMESPACE_PREFIX + Guid.NewGuid(), 1);
                    f.Dispose();
                }
                catch (UnauthorizedAccessException e)
                {
                    logger.Error(
                        "Cannot write into Global kernel namespace. Falling back to creating and opening objects without namespace prefix (so proper communication/synchronization is limit to just single windows session). To prevent this, make sure the process is running with appropriate privileges",
                        e);
                    return false;
                }

                return true;
            }
        }
    }
}
