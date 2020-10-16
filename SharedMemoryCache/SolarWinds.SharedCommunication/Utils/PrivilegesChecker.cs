using System;
using System.Collections.Generic;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SolarWinds.SharedCommunication.Utils
{
    public static class PrivilegesChecker
    {
        public static readonly bool CanWriteToGlobalNamespace = CanCreateMmfInGlobalNamespace();

        public static readonly string KernelObjectsPrefix =
            CanWriteToGlobalNamespace ? _GLOBAL_NAMESPACE_PREFIX : string.Empty;

        private const string _GLOBAL_NAMESPACE_PREFIX = "Global\\";

        private static bool CanCreateMmfInGlobalNamespace()
        {
            try
            {
                var f = MemoryMappedFile.CreateNew(_GLOBAL_NAMESPACE_PREFIX + Guid.NewGuid(), 1);
                f.Dispose();
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }

            return true;
        }
    }
}
