// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Rendering.Model.for_RenderScopeSelection;

public class when_selecting_scopes_like_the_axon_original : given.a_scoped_render_request
{
    [Fact]
    void should_select_every_slice_for_application_scope() =>
        RenderScopeSelection.Select(_request).Count.ShouldEqual(RenderScopeSelection.Locate(_application).Count());

    [Fact]
    void should_select_only_slices_in_a_module()
    {
        var module = _application.Modules[0];
        var selection = RenderScopeSelection.Select(_request with { Scope = new(ArtifactRenderScopeKind.Module, module.Id) });
        selection.ShouldNotBeEmpty();
        selection.All(slice => slice.Module.Id == module.Id).ShouldBeTrue();
    }

    [Fact]
    void should_select_only_slices_in_a_feature()
    {
        var feature = _application.Modules[0].Features[0];
        var selection = RenderScopeSelection.Select(_request with { Scope = new(ArtifactRenderScopeKind.Feature, feature.Id) });
        selection.ShouldNotBeEmpty();
        selection.All(slice => slice.Features.Any(ancestor => ancestor.Id == feature.Id)).ShouldBeTrue();
    }

    [Fact]
    void should_select_one_slice()
    {
        var slice = RenderScopeSelection.Locate(_application).First();
        var selection = RenderScopeSelection.Select(_request with { Scope = new(ArtifactRenderScopeKind.Slice, slice.Slice.Id) });
        selection.Count.ShouldEqual(1);
        selection[0].Slice.Id.ShouldEqual(slice.Slice.Id);
    }

    [Fact]
    void should_union_overlapping_scopes_without_duplicate_slices()
    {
        var slice = RenderScopeSelection.Locate(_application).First();
        var moduleScope = new ArtifactRenderScope(ArtifactRenderScopeKind.Module, slice.Module.Id);
        var selection = RenderScopeSelection.Select(_request with
        {
            Scope = moduleScope,
            AdditionalScopes = [new(ArtifactRenderScopeKind.Feature, slice.Features[0].Id), new(ArtifactRenderScopeKind.Slice, slice.Slice.Id), moduleScope]
        });
        selection.Select(item => item.Slice.Id).Distinct().Count().ShouldEqual(selection.Count);
        selection.Count.ShouldEqual(RenderScopeSelection.Select(_request with { Scope = moduleScope }).Count);
    }

    [Fact]
    void should_keep_the_entire_nested_feature_path()
    {
        var module = _application.Modules[0];
        var feature = module.Features[0];
        var child = feature with { Id = SemanticId.Parse($"sem1:{new string('a', 64)}"), Name = "Child", Features = [] };
        var application = _application with { Modules = [module with { Features = [feature with { Slices = [], Features = [child] }] }] };
        var nested = RenderScopeSelection.Locate(application).First();
        nested.Features.Count.ShouldEqual(2);
        nested.Path.ShouldEqual(module.Name, feature.Name, child.Name, nested.Slice.Name);
    }
}
