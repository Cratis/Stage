// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using Cratis.Specifications;

namespace Cratis.Stage.Rendering.Cratis.for_CratisBackendApplicationScaffold;

static class ReleaseSpecificationMetadata
{
    internal static void Verify(string directory)
    {
        var assemblies = 0;
        foreach (var path in Directory.EnumerateFiles(directory, "*.dll", SearchOption.AllDirectories))
        {
            using var stream = File.OpenRead(path);
            using var pe = new PEReader(stream);
            if (!pe.HasMetadata) continue;
            assemblies++;
            var reader = pe.GetMetadataReader();
            foreach (var handle in reader.TypeDefinitions)
            {
                var type = reader.GetTypeDefinition(handle);
                IsSpecificationName(reader.GetString(type.Name)).ShouldBeFalse();
                IsSpecificationName(reader.GetString(type.Namespace)).ShouldBeFalse();
            }
            foreach (var handle in reader.AssemblyReferences)
            {
                var name = reader.GetString(reader.GetAssemblyReference(handle).Name);
                IsTestingAssembly(name).ShouldBeFalse();
            }
            IsTestingAssembly(Path.GetFileNameWithoutExtension(path)).ShouldBeFalse();
        }
        (assemblies > 0).ShouldBeTrue();
        File.Exists(Path.Combine(directory, "SmokeApp.dll")).ShouldBeTrue();
    }

    static bool IsSpecificationName(string name) => name.Split('.').Any(segment => segment.StartsWith("for_", StringComparison.Ordinal) || segment.StartsWith("when_", StringComparison.Ordinal));

    static bool IsTestingAssembly(string name) => name.StartsWith("xunit", StringComparison.OrdinalIgnoreCase) ||
        name.StartsWith("NSubstitute", StringComparison.OrdinalIgnoreCase) || name.StartsWith("Cratis.Specifications", StringComparison.OrdinalIgnoreCase) ||
        name.StartsWith("Cratis.Chronicle.Testing", StringComparison.OrdinalIgnoreCase) || name.StartsWith("Cratis.Arc.Chronicle.Testing", StringComparison.OrdinalIgnoreCase);
}
