using System.Reflection;
using System.Runtime.CompilerServices;

namespace DataTyped
{
    internal static class ModuleInitializer
    {
        [ModuleInitializer]
        internal static void Run()
        {
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
        }

        private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            var assemblyName = new AssemblyName(args.Name);

            var loadedAssembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == assemblyName.Name);
            if (loadedAssembly != null)
                return loadedAssembly;

            var fileName = assemblyName.Name + ".dll";
            using var resourceStream = typeof(ModuleInitializerAttribute).Assembly.GetManifestResourceStream(fileName);
            if (resourceStream == null)
            {
                throw new InvalidDataException($"Resource: {fileName} not found.");
            }

            using var memoryStream = new MemoryStream();
            resourceStream.CopyTo(memoryStream);

            return Assembly.Load(memoryStream.ToArray());
        }
    }
}


