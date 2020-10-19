using System;
using System.Collections.Generic;
using System.Text;

namespace SolarWinds.SharedCommunication.Contracts.Utils
{
    public interface IKernelObjectsPrivilegesChecker
    {
        bool CanWriteToGlobalNamespace { get; }
        string KernelObjectsPrefix { get; }
    }
}
