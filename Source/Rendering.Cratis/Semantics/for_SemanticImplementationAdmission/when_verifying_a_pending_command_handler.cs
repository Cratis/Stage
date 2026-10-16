// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticImplementationAdmission;

public class when_verifying_a_pending_command_handler : Specification
{
    SemanticImplementationRequirement _requirement = null!;
    ImmutableArray<ArtifactRenderDiagnostic> _diagnostics;

    void Because()
    {
        var vector = DescribedCommandHandlersCorpus.V23;
        var form = vector.SourceForms.Single(source => source.Name == "single");
        var catalog = SemanticIdentityCatalogSerializer.Deserialize(form.IdentityCatalogBytes.AsSpan());
        var documents = form.Documents.Select(document => SemanticSourceDocument.Create(catalog.ResolveDocument(document.StableKey), document.StableKey, document.DisplayPath, document.Text));
        var compilation = new SemanticModelCompiler().Compile(vector.ApplicationName, SemanticDocumentSet.Create([.. documents], catalog));
        Assert.True(compilation.Success, string.Join(Environment.NewLine, compilation.Diagnostics));
        var model = compilation.Value!.Model;
        _requirement = compilation.ImplementationRequirements.Single();
        var request = new ArtifactRenderRequest(model, SemanticExecutionPlan.Compile(model, []).Plan!, CratisRendering.CreateProfile(vector.ApplicationName, new("Described", "Described")), new(ArtifactRenderScopeKind.Application, model.Application.Id))
        {
            ImplementationRequirements = compilation.ImplementationRequirements,
            ImplementationContents = []
        };
        _diagnostics = SemanticImplementationAdmission.Verify(request);
    }

    [Fact] void should_receive_a_pending_requirement() => _requirement.AttachmentResolution.ShouldEqual(SemanticAttachmentResolution.Pending);
    [Fact] void should_refuse_with_the_described_handler_code() => _diagnostics.Single().Code.ShouldEqual("STAGE-IMPL-002");
    [Fact] void should_name_the_requirement() => _diagnostics.Single().Message.ShouldContain(_requirement.RequirementId);
}
