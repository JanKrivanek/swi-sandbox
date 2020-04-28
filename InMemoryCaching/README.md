# Redis In Memory Caching Prototype - 

## Introduction

In Orion we use in-memory caching between multiple calls accross the entire stack (Collector ETL logic; website presentation; DAL of Business layer; etc.)
This prototype shows possible usage of `Microsoft.Extensions.Caching.Abstractions` in implementing in-memory storages of state within types taken from Orion (specifically NPM). The abstraction is then backed up (via DI) by `Microsoft.Extensions.Caching.Redis`

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
