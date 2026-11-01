// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Rendering.Model.for_RenderModelBuilder;

public class when_building_an_empty_application : given.a_response_only_command
{
    void Because()
    {
        var application = _request.Model.Application with { Modules = [] };
        var model = ExecutableSemanticModel.Create(LanguageVersion.V1, SemanticVersion.V1, application);
        _result = RenderModelBuilder.Build(_request with { Model = model, Scope = new(ArtifactRenderScopeKind.Application, application.Id) });
    }

    [Fact] void should_have_no_commands() => _result.Items.Commands.ShouldBeEmpty();
    [Fact] void should_have_no_reactions() => _result.Items.Reactions.ShouldBeEmpty();
    [Fact] void should_have_no_slices() => _result.Scope.Slices.ShouldBeEmpty();
    [Fact] void should_keep_application_identity() => _result.Source.ApplicationId.ShouldEqual(_request.Model.Application.Id);
}
