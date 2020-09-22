dotnet nuget push -s http://10.150.196.54:5555/v3/index.json -k solarwinds-netman-nuget .\SolarWinds.SharedCommunication.Contracts\bin\Debug\*.nupkg
dotnet nuget push -s http://10.150.196.54:5555/v3/index.json -k solarwinds-netman-nuget .\SolarWinds.SharedCommunication\*.nupkg
