# Dependencies analyzer

Analyses dependencies of given assembly/assemblies on other assemblies, down to methods level

## Sample output

A csv file with dependencies

```csv
CallingModule,CalledModule,CalledNamespace,CalledType,CalledFunction,CalledFunctionSignature
SolarWinds.Administration.Contract.dll,SolarWinds.Administration.ProductCatalog.Model,SolarWinds.Administration.ProductCatalog.Model,ProductModel,get_Id,System.String SolarWinds.Administration.ProductCatalog.Model.ProductModel::get_Id()
SolarWinds.Administration.Contract.dll,SolarWinds.Administration.ProductCatalog.Model,SolarWinds.Administration.ProductCatalog.Model,ProductModel,get_Version,System.Version SolarWinds.Administration.ProductCatalog.Model.ProductModel::get_Version()
```

## Usage

```console
DependencyAnalyzer.exe <folder with assemblies to analyze>|<assembly file to analyze>
```

Point the analyzer tool to an assembly file or folder with assembles, that you want to analyse. Preferabely those are build in debug mode and symbol files (.pdb) are in the same folder.
Tool will analyze all assemblies with 'SolarWinds' in their name and will analyze only dependecies on assemblies with 'SolarWinds' in their name (both case insensitive). It will skip dependencies within single assembly and deduplicate the results

## Inner logic
Tool iterates over all methods within all types within given assembly/assemblies and for each method inspect their IL body. It looks for method call instructions (Call, CallVirt, Calli) and extracts the method information for the call target. This way we can build dependency map of our assembly on types and methods from referenced assemblies.