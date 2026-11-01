// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisSemanticInventory.when_creating;

public class with_two_roots_for_one_dependent : given.an_inventory_request
{
    ArtifactRenderDegradation _dependent = null!;
    ImmutableArray<ArtifactRenderGapReference> _expected = [];

    void Establish()
    {
        var roots = new[] { _registration.Events.Single().Id, _renaming.Events.Single().Id }.OrderBy(id => id.ToString(), StringComparer.Ordinal).ToArray();
        _dependent = Evidence(_registration.Commands.Single().Id, ArtifactRenderDegradationCodes.DependencyDegradation);
        _causes[_dependent.Diagnostic.Artifact] = [roots[1], roots[0], roots[1]];
        var codes = new[] { ArtifactRenderDegradationCodes.UnknownOwnerMember, ArtifactRenderDegradationCodes.UnsupportedConstruct };
        _expected = [.. roots.SelectMany(root => codes.Select(code => new ArtifactRenderGapReference(root, code)))];
        var evidence = roots.Reverse().SelectMany(root => codes.Reverse().Select(code => Evidence(root, code))).Append(_dependent).ToImmutableArray();
        _plan = Plan(evidence, [.. evidence.GroupBy(gap => gap.Diagnostic.Artifact).Select(group => new ArtifactRenderRefusal(group.Key, group.Key == _dependent.Diagnostic.Artifact ? "command" : "event", [.. group.Select(gap => gap.Diagnostic.Code)]))]);
    }

    void Because() => _inventory = CreateInventory();

    [Fact] void should_sort_and_deduplicate_the_root_references() => _inventory.Constructs.Single(construct => construct.Identity == _dependent.Diagnostic.Artifact).Degradations.Single().Roots.SequenceEqual(_expected).ShouldBeTrue();
    [Fact] void should_pass_plan_inventory_validation() => Catch.Exception(() => _plan.WithInventory(_inventory)).ShouldBeNull();
}
