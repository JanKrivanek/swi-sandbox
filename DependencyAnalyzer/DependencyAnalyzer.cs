using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace DependencyAnalyzer
{
    public class DependencyAnalyzer : IDisposable
    {
        private readonly StreamWriter _csv;
        private readonly HashSet<string> _foundMethods = new HashSet<string>();

        public const string CALLING_ASSEMBLY_MANDATORY_NAME_PATTERN = "Solarwinds";
        public const string CALLED_ASSEMBLY_MANDATORY_NAME_PATTERN = "Solarwinds";

        public DependencyAnalyzer(string csvPath)
        {
            _csv = new StreamWriter(csvPath);
        }

        private static IEnumerable<MethodReference> GetMethodsCalled(
            MethodDefinition caller)
        {
            //If for some reason loading assembly without symbols or with release symbols - we can miss bodies of some methods
            // This is a limitation of used reflection tool - as IL should have all info; however we are OK with this in our situation
            // as we can just rebuild the inspected project in debug mode and inspect then
            if (!caller.HasBody)
            {
                return Enumerable.Empty<MethodReference>();
            }

            return caller.Body.Instructions
                .Where(x => x.OpCode == OpCodes.Call || x.OpCode == OpCodes.Callvirt || x.OpCode == OpCodes.Calli)
                .Select(x => (MethodReference)x.Operand);
        }

        /// <summary>
        /// Analyses the given assembly (or directory) and spits the result file
        /// </summary>
        /// <param name="assemblyPath"></param>
        public void Analyze(string assemblyPath)
        {
            _csv.WriteLine(
                "CallingModule,CalledModule,CalledNamespace,CalledType,CalledFunction,CalledFunctionSignature");

            if (File.Exists(assemblyPath))
            {
                AnalyzeSingle(assemblyPath);
            }
            else if (Directory.Exists(assemblyPath))
            {
                foreach (string file in Directory.EnumerateFiles(assemblyPath,
                    "*" + CALLING_ASSEMBLY_MANDATORY_NAME_PATTERN + "*.dll", SearchOption.AllDirectories))
                {
                    AnalyzeSingle(file);
                }
            }
            else
            {
                throw new ArgumentException("Given path is not recognized neither as valid file nor a valid directory");
            }
        }

        private void AnalyzeSingle(string assemblyPath)
        {
            ModuleDefinition module = null;
            try
            {
                module = ModuleDefinition.ReadModule(assemblyPath);
            }
            catch (BadImageFormatException e)
            {
                return;
            }
             
            ////skip invalid modules (files that are not .net assemblies)
            //// and modules without symbols - for those we cannot read the method bodies properly
            //if (module == null || !module.HasSymbols)
            //{
            //    return;
            //}

            foreach (TypeDefinition type in module.Types)
            {
                foreach (MethodDefinition method in type.Methods)
                {
                    IEnumerable<MethodReference> calledMethods = GetMethodsCalled(method);

                    foreach (MethodReference calledMethod in calledMethods)
                    {
                        //BEWARE!: calledMethod.DeclaringType.Module.Name - points to self
                        string calledAssemblyName = calledMethod.DeclaringType.Scope.Name;

                        if (
                            //skipping calls within single assembly
                            calledAssemblyName != module.Name
                            &&
                            //skipping calls not matching pattern
                            calledMethod.DeclaringType.Scope.Name.IndexOf(CALLED_ASSEMBLY_MANDATORY_NAME_PATTERN,
                                StringComparison.InvariantCultureIgnoreCase) >= 0
                            &&
                            //skipping already printed calls
                            !_foundMethods.Contains(calledMethod.FullName)
                            )
                        {
                            _foundMethods.Add(calledMethod.FullName);

                            _csv.WriteLine(
                                $"{module.Name},{calledAssemblyName},{calledMethod.DeclaringType.Namespace},{calledMethod.DeclaringType.Name},{calledMethod.Name},{calledMethod.FullName}");
                        }
                    }
                }
            }
        }

        public void Dispose()
        {
            _csv?.Flush();
            _csv?.Dispose();
        }
    }
}
