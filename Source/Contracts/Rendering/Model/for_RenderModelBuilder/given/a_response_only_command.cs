// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Rendering.Model.for_RenderModelBuilder.given;

public class a_response_only_command : Specification
{
    private protected ArtifactRenderRequest _request = null!;
    private protected SemanticSlice _slice = null!;
    private protected SemanticCommand _command = null!;
    private protected RenderModel _result = null!;

    private protected static SemanticId Id(string key) => SemanticId.Create(SemanticKind.Property, key);

    private protected ArtifactRenderRequest WithApplication(SemanticApplication application) => _request with
    {
        Model = ExecutableSemanticModel.Create(_request.Model.LanguageVersion, _request.Model.SemanticVersion, application)
    };

    private protected ArtifactRenderRequest WithSlice(SemanticSlice slice)
    {
        var module = _request.Model.Application.Modules[0];
        var feature = module.Features[0] with { Slices = [slice] };

        return WithApplication(_request.Model.Application with { Modules = [module with { Features = [feature] }] });
    }

    private protected static ArtifactRenderRequest CompileRequest(string source)
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Sample"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("response"), "response", "response.play", source);
        var compilation = new SemanticModelCompiler().Compile("Sample", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join(Environment.NewLine, compilation.Diagnostics));
        var model = compilation.Value!.Model;
        var profile = ArtifactRenderProfile.Create("cratis", "1", "Cratis.Stage.Rendering.Cratis", "1", []);
        return new(model, SemanticExecutionPlan.Compile(model).Plan!, profile, new(ArtifactRenderScopeKind.Application, model.Application.Id));
    }

    void Establish()
    {
        _request = CompileRequest("""
            module Messaging
              feature Echoes
                slice StateChange Reply
                  command Echo
                    message String
                    returns message
            """);
        _slice = _request.Model.Application.Modules[0].Features[0].Slices[0];
        _command = _slice.Commands[0];
        _request = _request with { Scope = new(ArtifactRenderScopeKind.Slice, _slice.Id) };
    }
}
