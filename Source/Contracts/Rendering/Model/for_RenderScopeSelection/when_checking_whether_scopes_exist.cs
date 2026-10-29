// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Rendering.Model.for_RenderScopeSelection;

public class when_checking_whether_scopes_exist : given.a_scoped_render_request
{
    SemanticId _unknown;
    void Establish() => _unknown = _savings.Slices[0].Id;
    bool Exists(ArtifactRenderScopeKind kind, SemanticId artifact) => RenderScopeSelection.Exists(_request, new(kind, artifact));

    [Fact] void should_find_the_application() => Exists(ArtifactRenderScopeKind.Application, _application.Id).ShouldBeTrue();
    [Fact] void should_find_a_module() => Exists(ArtifactRenderScopeKind.Module, _banking.Id).ShouldBeTrue();
    [Fact] void should_find_a_nested_feature() => Exists(ArtifactRenderScopeKind.Feature, _savings.Id).ShouldBeTrue();
    [Fact] void should_find_a_slice() => Exists(ArtifactRenderScopeKind.Slice, _savings.Slices[0].Id).ShouldBeTrue();
    [Fact] void should_not_find_a_slice_as_a_module() => Exists(ArtifactRenderScopeKind.Module, _unknown).ShouldBeFalse();
    [Fact] void should_not_find_a_module_as_a_feature() => Exists(ArtifactRenderScopeKind.Feature, _banking.Id).ShouldBeFalse();
    [Fact] void should_not_find_a_feature_as_a_slice() => Exists(ArtifactRenderScopeKind.Slice, _accounts.Id).ShouldBeFalse();
    [Fact] void should_not_find_a_module_as_the_application() => Exists(ArtifactRenderScopeKind.Application, _banking.Id).ShouldBeFalse();
}
