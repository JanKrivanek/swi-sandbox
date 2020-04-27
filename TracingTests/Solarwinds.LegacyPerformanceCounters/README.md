# SolarWinds.PerformanceCounters

## Introduction

**SolarWinds.PerformanceCounters** is a redirection code for functionality depending on `System.Diagnostics.PerformanceCounter`. It's meant to speed up the Slingshot onboarding process by not having to touch existing code that's using the PerformanceCounters.
New code should use different method of exposing metrics.
Not all functionality of PerformanceCounters is exposed (e.g. CounterSample functionality is not yet available)
Redirection of SolarWinds internal wrappers of performance counters (`SolarWinds.Common.Diagnostics.PerformanceCounters`, `SolarWinds.Orion.Common.SWPerfMonCounters` etc.) is planned.

## Usage
Once current packafe is referenced no other work is needed (as currently `System.Diagnostics` namespace is intentionaly used).

## Usage in full .NET Framework
Should this package be needed to be referenced from full .NET Framework assembly - conflicts with Microsoft BCL `System.Diagnostics` namespace can occur. In such a case we need to use explicit extern alias for the nuget package (detailed howto: https://github.com/NuGet/Home/issues/4989)
Once the alias is defined (distinct from `global` alias), then any reference to 'System.Diagnostics.PerformanceCounter' is linked to Microsoft BCL. Should we want to link it to our functionality, we need to explicitly use the alis in all code files using the functionality:

```cs
extern alias SwPerfCounters;

using SwPerfCounters::System.Diagnostics;

//Rest code stays as is - System.Diagnostics now first tries to resolve to our reference
```
