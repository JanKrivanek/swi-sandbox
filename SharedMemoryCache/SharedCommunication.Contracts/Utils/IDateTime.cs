using System;

namespace SolarWinds.SharedCommunication.Contracts.Utils
{
    public interface IDateTime
    {
        DateTime UtcNow { get; }
    }
}