// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticDependencyIndex;

public class when_auditing_executable_edge_witnesses : Specification
{
    SemanticDependencyKind[] _missing = [];
    SemanticDependencyEdge[] _invalid = [];

    static bool IsWitness(Type type) => type.IsSubclassOf(typeof(given.a_dependency_index)) && !type.IsAbstract;

    static IEnumerable<Type> Witnesses() => typeof(given.a_dependency_index).Assembly.GetTypes()
        .Where(type => IsWitness(type) && type.Namespace == typeof(when_finding_dependents.of_an_event_consumed_by_another_slice_projection).Namespace)
        .OrderBy(type => type.FullName, StringComparer.Ordinal);

    async Task Because()
    {
        var edges = new List<SemanticDependencyEdge>();
        foreach (var type in Witnesses())
        {
            var witness = (given.a_dependency_index)Activator.CreateInstance(type)!;
            try
            {
                await witness.InitializeAsync();
                if (witness._index.IsSemanticGraphComplete) edges.AddRange(witness._index.Edges);
            }
            finally
            {
                await witness.DisposeAsync();
            }
        }
        _missing = [.. Enum.GetValues<SemanticDependencyKind>().Except(edges.Select(edge => edge.Kind))];
        _invalid = [.. edges.Where(edge => !edge.Dependent.IsSet || !edge.Dependency.IsSet || edge.Dependent == edge.Dependency)];
    }

    [Fact] void should_have_executable_edges_for_every_supported_kind() => Assert.True(_missing.Length == 0, string.Join(", ", _missing));
    [Fact] void should_use_resolved_distinct_owners_in_the_witnesses() => _invalid.ShouldBeEmpty();
}
