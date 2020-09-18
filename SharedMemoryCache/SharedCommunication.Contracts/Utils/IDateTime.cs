using System;

namespace SharedCommunication.Contracts.Utils
{
    public interface IDateTime
    {
        DateTime UtcNow { get; }
    }
}