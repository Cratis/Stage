// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.Json;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Rendering.for_ArtifactRenderInventory;

public class when_hashing_scopes : given.an_inventory
{
    ImmutableArray<ArtifactRenderScope> _scopes;

    void Establish()
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("source"), "source", "source.play", """
            concept ProjectId : Uuid
            module Projects
              feature Registration
                slice StateChange First
                  command First
                    projectId ProjectId identifier
                    produces FirstRegistered
                      projectId = projectId
                  event FirstRegistered
                    projectId ProjectId
                slice StateChange Second
                  command Second
                    projectId ProjectId identifier
                    produces SecondRegistered
                      projectId = projectId
                  event SecondRegistered
                    projectId ProjectId
                slice StateChange Third
                  command Third
                    projectId ProjectId identifier
                    produces ThirdRegistered
                      projectId = projectId
                  event ThirdRegistered
                    projectId ProjectId
            """);
        var model = new SemanticModelCompiler().Compile("Projects", SemanticDocumentSet.Create([document], catalog)).Value!.Model;
        _scopes = [.. model.Application.Modules.Single().Features.Single().Slices.Select(slice => new ArtifactRenderScope(ArtifactRenderScopeKind.Slice, slice.Id))];
        _request = _request with { Model = model, ExecutionPlan = SemanticExecutionPlan.Compile(model).Plan!, Scope = _scopes[0] };
        var pair = new ArtifactRenderVersionPair(model.LanguageVersion, model.SemanticVersion);
        _context = _context with { Input = pair, Support = new([pair]) };
    }

    [Fact]
    void should_change_plan_and_report_hashes_when_the_selected_scope_set_changes()
    {
        var first = Scoped(0, 1);
        var expanded = Scoped(0, 1, 2);
        first.Inventory!.Digest.ShouldEqual(expanded.Inventory!.Digest);
        first.Digest.ShouldNotEqual(expanded.Digest);
        var before = ArtifactRenderReports.Create(first, _context);
        var after = ArtifactRenderReports.Create(expanded, _context);
        before.Completeness.Sha256.ShouldNotEqual(after.Completeness.Sha256);
        before.Unsupported.Sha256.ShouldNotEqual(after.Unsupported.Sha256);
        using var json = JsonDocument.Parse(after.Completeness.Bytes.AsMemory());
        json.RootElement.GetProperty("scopes").GetArrayLength().ShouldEqual(3);
    }

    [Fact]
    void should_normalize_additional_scope_order()
    {
        var first = Scoped(0, 1, 2);
        var reordered = Scoped(0, 2, 1);
        AssertSameSelection(first, reordered);
    }

    [Fact]
    void should_treat_the_same_primary_and_additional_scope_union_as_the_same_selection()
    {
        var first = Scoped(0, 1, 2);
        var reordered = Scoped(1, 2, 0);
        AssertSameSelection(first, reordered);
    }

    ArtifactRenderPlan Scoped(int primary, params int[] additional) => ArtifactRenderPlan.Create(
        _request with { Scope = _scopes[primary], AdditionalScopes = [.. additional.Select(index => _scopes[index])] },
        [],
        []).WithInventory(ArtifactRenderInventory.Create([]));

    void AssertSameSelection(ArtifactRenderPlan expected, ArtifactRenderPlan actual)
    {
        actual.Digest.ShouldEqual(expected.Digest);
        var first = ArtifactRenderReports.Create(expected, _context);
        var second = ArtifactRenderReports.Create(actual, _context);
        second.Completeness.Bytes.SequenceEqual(first.Completeness.Bytes).ShouldBeTrue();
        second.Unsupported.Bytes.SequenceEqual(first.Unsupported.Bytes).ShouldBeTrue();
    }
}
