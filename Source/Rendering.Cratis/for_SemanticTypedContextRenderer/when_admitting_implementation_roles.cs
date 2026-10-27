// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_SemanticTypedContextRenderer;

public class when_admitting_implementation_roles : Specification
{
    ArtifactRenderRequest _request = null!;

    void Establish()
    {
        const string source = """
            concept Code : String
              validate csharp
                ```csharp
                return true;
                ```
            module Projects
              feature Lookup
                slice StateView Codes
                  readmodel CodeRow
                    id Uuid key
                    code Code
                  query All => CodeRow[]
            """;
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("application"), "application", "application.play", source);
        var compiled = new SemanticModelCompiler().Compile("Projects", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compiled.Success, string.Join("; ", compiled.Diagnostics));
        var model = compiled.Value!.Model;
        _request = new(
            model,
            SemanticExecutionPlan.Compile(model).Plan!,
            CratisRendering.CreateProfile("Projects", new("Projects", "Projects")),
            new(ArtifactRenderScopeKind.Application, model.Application.Id))
        {
            ImplementationRequirements = compiled.ImplementationRequirements,
            TypedContextDescriptors = compiled.TypedContextDescriptors
        };
    }

    [Fact]
    void should_classify_every_role_in_exactly_one_set()
    {
        foreach (var role in Enum.GetValues<SemanticImplementationRole>())
        {
            var required = SemanticTypedContextAdmission.RequiresDescriptor(role);
            var exempt = SemanticTypedContextAdmission.IsDescriptorExempt(role);
            (required ^ exempt).ShouldBeTrue();
        }
    }

    [Fact]
    void should_require_exactly_screenplays_strict_descriptor_roles()
    {
        // Screenplay Documentation/screenplay/context.md and SemanticTypedContextCatalog's strict check.
        // The catalog and its inline strict-role list are internal to Screenplay, so pin the contract here.
        SemanticImplementationRole[] expected =
        [
            SemanticImplementationRole.CommandHandler,
            SemanticImplementationRole.CommandValidation,
            SemanticImplementationRole.ConceptValidation,
            SemanticImplementationRole.RulePredicate,
            SemanticImplementationRole.ReducerTransition,
            SemanticImplementationRole.ValueGeneration
        ];
        var required = Enum.GetValues<SemanticImplementationRole>().Where(SemanticTypedContextAdmission.RequiresDescriptor).ToHashSet();
        required.SetEquals(expected).ShouldBeTrue();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    void should_refuse_an_unknown_role_even_without_descriptors(bool includeDescriptors)
    {
        _request.TypedContextDescriptors.ShouldNotBeEmpty();
        var unknown = _request.ImplementationRequirements.Single() with
        {
            Role = (SemanticImplementationRole)int.MaxValue,
            RequirementId = "unknown-role"
        };
        var request = _request with
        {
            ImplementationRequirements = _request.ImplementationRequirements.Add(unknown),
            TypedContextDescriptors = includeDescriptors ? _request.TypedContextDescriptors : []
        };
        var diagnostic = Assert.Single(SemanticTypedContextAdmission.Verify(request));
        diagnostic.Code.ShouldEqual("STAGE-ESM-021");
        diagnostic.Severity.ShouldEqual(ArtifactRenderDiagnosticSeverity.Error);
        diagnostic.Message.ShouldEqual($"Implementation requirement '{unknown.RequirementId}' has role '{unknown.Role}', which this Stage version does not support for typed-context admission.");
    }
}
