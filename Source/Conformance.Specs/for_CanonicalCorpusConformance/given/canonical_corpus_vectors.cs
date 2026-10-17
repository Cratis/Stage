// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Collections.Immutable;
using System.Reflection;
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Stage.Conformance.Specs.for_CanonicalCorpusConformance.given;

// Enumerates the corpus package by reflection: the package has no central vector index, so a hand list would rot.
public static class canonical_corpus_vectors
{
    const BindingFlags PublicStatic = BindingFlags.Public | BindingFlags.Static;

    public static Assembly CorpusAssembly => typeof(EsmGoldenVectors).Assembly;

    public static IReadOnlyList<Type> CorpusTypes { get; } = [.. CorpusAssembly.GetExportedTypes().Where(type => type.IsAbstract && type.IsSealed).OrderBy(type => type.Name, StringComparer.Ordinal)];

    public static IReadOnlyList<conformance_vector> All { get; } = Enumerate();

    public static IReadOnlyList<string> TypesWithoutVectors { get; } =
        [.. CorpusTypes.Select(type => type.Name).Except(All.Select(vector => vector.Source), StringComparer.Ordinal)];

    static List<conformance_vector> Enumerate()
    {
        var vectors = new List<conformance_vector>();
        foreach (var type in CorpusTypes.Where(type => type != typeof(EsmGoldenVectors)))
        {
            foreach (var property in type.GetProperties(PublicStatic).Where(property => property.GetIndexParameters().Length == 0))
            {
                var value = property.GetValue(null);
                var found = value switch
                {
                    CanonicalCorpusVector vector => [vector],
                    ImmutableArray<CanonicalCorpusVector> many => many.ToArray(),
                    _ => []
                };
                vectors.AddRange(found.Select((vector, index) => new conformance_vector(
                    found.Length == 1 ? $"{type.Name}.{property.Name}" : $"{type.Name}.{property.Name}[{index}]",
                    type.Name,
                    SemanticModelSerializer.Deserialize(vector.EsmBytes.AsSpan()),
                    vector)));
            }
        }

        // Every full ESM golden is a vector too, so a new version file in the package is a new row without a code change.
        vectors.AddRange(EsmGoldenVectors.Versions.Select(version => Golden($"{nameof(EsmGoldenVectors)}.V{version}", EsmGoldenVectors.Get(version))));
        vectors.AddRange(typeof(EsmGoldenVectors).GetProperties(PublicStatic)
            .Where(property => property.PropertyType == typeof(EsmGoldenVector))
            .Select(property => Golden($"{nameof(EsmGoldenVectors)}.{property.Name}", (EsmGoldenVector)property.GetValue(null)!)));
        return vectors;
    }

    static conformance_vector Golden(string name, EsmGoldenVector golden) =>
        new(name, nameof(EsmGoldenVectors), SemanticModelSerializer.Deserialize(golden.Bytes.AsSpan()), null);
}
#endif
