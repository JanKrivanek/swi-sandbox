# Redis In Memory Caching Prototype

## Introduction

In Orion we use in-memory caching between multiple calls accross the entire stack (Collector ETL logic; website presentation; DAL of Business layer; etc.)
This prototype shows possible usage of `Microsoft.Extensions.Caching.Abstractions` in implementing in-memory storages of state within types taken from Orion (specifically NPM). The abstraction is then backed up (via DI) by `Microsoft.Extensions.Caching.Redis`

## Scope

This prototype focuses on sample backing of in-memory state caching of data in NPM via [IObjectCache](https://bitbucket.solarwinds.com/projects/PAC/repos/interfaces/browse/Src/Web2/SolarWinds.Orion.Interfaces.WebApi/Caching/System/IObjectCache.cs#9), [EntityManager](https://bitbucket.solarwinds.com/projects/PLATFORM/repos/collector/browse/Src/Lib/SolarWinds.Collector/EntityManager.cs#11) etc. However same concepts are generaly applicable to any state caching accross Orion
It uses abstractions for backing the caching and de/serialization of the data; data are strongly typed.

## Out of scope

Handling of failures and recovery - there is a sample example of distributed cache failing over to (and back from) local in-memory cache; but production logic would need to be adjusted to actual scenarios (do we need 100% consistency accross consumers? Or do we actually just cache persisted immutable state? Can we fall-back to partitioned caching and if yes, do we need to consolidate data after shared cache is again available? etc.)
Expiration, invalidation etc. - this is left out to user code - as again it depends on actual scenarios of consuming code

## How to run/debug

To build the sample application and run it in composition of containers (3 producers/consumers; 1 Redis cache) you can run
```powershell
> RunCompose.ps1
```

This will build the docker image of the consumer app (with the sample caching library); spin up 3 containers from that image and 1 container with the Redis cache

## Sample caches implementations

`ObjectCacheFromMemoryCache` - caches the data within the running process (via the injected `IMemoryCache` implementation)
`ObjectCacheFromDistributed` - caches the data in distributed cache (via the injected `IDistributedCache` implementation)
`ObjectCacheFromDistributedWithFallbackToLocal` - caches the data in distributed cache, with fallback to local cache in case of errors
