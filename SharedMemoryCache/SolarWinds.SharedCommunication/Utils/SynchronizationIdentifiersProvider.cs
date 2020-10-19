using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using SolarWinds.SharedCommunication.Contracts.Utils;

namespace SolarWinds.SharedCommunication.Utils
{
    public class SynchronizationIdentifiersProvider: ISynchronizationIdentifiersProvider
    {
        public string GetSynchronizationIdentifier(string apiBaseAddress, string apiKey, string orgId)
        {
            if (string.IsNullOrEmpty(apiBaseAddress))
                throw new ArgumentException("Parameter must be nonempty", nameof(apiBaseAddress));

            if (string.IsNullOrEmpty(apiKey))
                throw new ArgumentException("Parameter must be nonempty", nameof(apiKey));

            string uniqueIdentity = apiBaseAddress + "_" + apiKey + (orgId == null ? null : ("_" + orgId));
            //it's better to randomize salt; on the other hand we must get consistent result across processes
            // so some common schema must be used. No salting might be acceptable as well - the identity
            // should be long and random enough to prevent against hashed dictionary attack.
            string salt = "k(Dcvw,F5LK;*[K~";

            //now hash to prevent info leaking
            var hashAlgo = new SHA256Managed();
            var hash = hashAlgo.ComputeHash(Encoding.UTF8.GetBytes(uniqueIdentity + salt));
            //this can now be used as identity of shared handles (memory mapped files etc.)
            string id = Convert.ToBase64String(hash);

            return id;
        }

        public string GetSynchronizationIdentifier(string apiBaseAddress, string apiKey)
        {
            return GetSynchronizationIdentifier(apiBaseAddress, apiKey, null);
        }
    }
}
