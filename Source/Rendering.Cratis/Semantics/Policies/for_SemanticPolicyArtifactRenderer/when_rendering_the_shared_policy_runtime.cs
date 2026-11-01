// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.Policies.for_SemanticPolicyArtifactRenderer;

public class when_rendering_the_shared_policy_runtime : for_SemanticPolicyNames.given.a_policy_application
{
    RenderedFile[] _files = [];
    string _runtime = string.Empty;

    void Establish() => Compile(Module("Billing", Feature("Invoicing", Slice("IssueInvoice", "Staff"))));

    void Because()
    {
        CratisRenderingOptions options = new("InvoiceApp", "Invoices");
        var context = new SemanticApplicationContext(
            new(
                _model,
                SemanticExecutionPlan.Compile(_model).Plan!,
                CratisRendering.CreateProfile(_model.Application.Name, options),
                new(ArtifactRenderScopeKind.Application, _model.Application.Id)),
            options);
        _files = [SemanticPolicyArtifactRenderer.RenderShared(context), new("Registration.cs", """
            namespace Invoices.GeneratedPolicies;
            public static partial class Registration
            {
                static partial void RegisterGenerated(global::Microsoft.Extensions.DependencyInjection.IServiceCollection services);
            }
            """)];
        _runtime = _files.Single(file => file.RelativePath.EndsWith("Policies.cs", StringComparison.Ordinal)).Content;
    }

    [Fact]
    void should_compare_concept_bases_by_type()
    {
        _runtime.ShouldContain("typeof(ConceptAs<>)");
        _runtime.ShouldContain("typeof(EventSourceId<>)");
    }

    [Fact] void should_not_compare_type_names_as_strings() => _runtime.ShouldNotContain("`1\"");
    [Fact] void should_compile() => string.Join(Environment.NewLine, RenderedOutput.Errors(_files)).ShouldEqual(string.Empty);
}
#endif
