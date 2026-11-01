// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticDependencyIndex;

public class when_auditing_reference_classifications : for_SemanticSurfaceLedger.given.the_executable_surface
{
    string[] _unclassified = [];
    string[] _unexplained = [];

    static IEnumerable<string> TypedContextMembers()
    {
        Type[] types = [typeof(SemanticTypedContextDescriptor), typeof(SemanticTypedContextMember), typeof(SemanticContextType), typeof(SemanticContextSource), typeof(SemanticContextTypeDefinition), typeof(SemanticContextProperty)];
        return types.SelectMany(type => SemanticDependencySurface.Properties(type).Select(property => $"{type.Name}.{property.Name}"));
    }

    void Because()
    {
        var members = EnumerateSurface().Concat(TypedContextMembers()).ToArray();
        _unclassified = [.. members.Where(member => SemanticDependencySurface.Classify(member) is null)];
        _unexplained = [.. members.Where(member => SemanticDependencySurface.Classify(member) is { Kind: null, NonReferenceReason: null or "" })];
    }

    [Fact] void should_classify_every_reference_carrying_surface_member() => Assert.True(_unclassified.Length == 0, string.Join(Environment.NewLine, _unclassified));
    [Fact] void should_explain_members_without_owner_references() => _unexplained.ShouldBeEmpty();
    [Fact] void should_not_classify_an_unrecognized_reference_member() => SemanticDependencySurface.Classify("SemanticCommand.FutureReadModel").ShouldBeNull();
    [Fact] void should_not_expose_unqualified_completeness() => typeof(SemanticDependencyIndex).GetProperty("IsComplete", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).ShouldBeNull();
    [Fact] void should_explicitly_leave_syntax_only_scenes_outside_the_index() => SemanticDependencyIndex.OutOfIndex.Any(reason => reason.StartsWith("Screens/navigation:", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_explicitly_exclude_seeds_outside_esm() => SemanticDependencyIndex.OutOfIndex.Any(reason => reason.StartsWith("Seeds:", StringComparison.Ordinal)).ShouldBeTrue();
}
