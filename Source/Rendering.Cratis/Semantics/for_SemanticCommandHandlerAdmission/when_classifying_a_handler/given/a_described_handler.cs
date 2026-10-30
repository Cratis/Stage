// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;
using static Cratis.Stage.Rendering.Cratis.Semantics.SemanticCommandHandlerAdmission;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticCommandHandlerAdmission.when_classifying_a_handler.given;

public class a_described_handler : Specification
{
    protected SemanticCommand _command = null!;
    protected SemanticImplementationRequirement _requirement = null!;
    protected ArtifactRenderRequest _request = null!;
    internal Classification _classification = null!;

    void Establish()
    {
        var vector = DescribedCommandHandlersCorpus.V23;
        var form = vector.SourceForms.Single(source => source.Name == "single");
        var catalog = SemanticIdentityCatalogSerializer.Deserialize(form.IdentityCatalogBytes.AsSpan());
        var documents = form.Documents.Select(document => SemanticSourceDocument.Create(catalog.ResolveDocument(document.StableKey), document.StableKey, document.DisplayPath, document.Text));
        var compilation = new SemanticModelCompiler().Compile(vector.ApplicationName, SemanticDocumentSet.Create([.. documents], catalog));
        Assert.True(compilation.Success, string.Join(Environment.NewLine, compilation.Diagnostics));
        var model = compilation.Value!.Model;
        _command = model.Application.Modules.SelectMany(module => module.Features).SelectMany(feature => feature.Slices).SelectMany(slice => slice.Commands).Single(command => command.Handler is not null);
        _requirement = compilation.ImplementationRequirements.Single();
        _request = new(model, SemanticExecutionPlan.Compile(model, []).Plan!, CratisRendering.CreateProfile(vector.ApplicationName, new("Described", "Described")), new(ArtifactRenderScopeKind.Application, model.Application.Id))
        {
            ImplementationRequirements = [_requirement]
        };
    }

    protected void Resolve(string? language, string? file = null) => SetRequirement(_requirement with
    {
        AttachmentResolution = SemanticAttachmentResolution.Resolved,
        Language = language,
        File = file
    });

    protected void SetRequirement(SemanticImplementationRequirement requirement)
    {
        _requirement = requirement;
        _request = _request with { ImplementationRequirements = [requirement] };
    }
}
