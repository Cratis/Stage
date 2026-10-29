// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticSpecificationArtifactRenderer;

public class when_rendering_a_policy_denial : Specification
{
    string _source = null!;

    void Because()
    {
        var source = "policy Clerks\n  require authenticated\n" + invoice_model.Source("String", invoice_model.TextSource, invoice_model.OtherTextSource)
            .Replace("command IssueInvoice\n", "command IssueInvoice\n        authorize Clerks\n", StringComparison.Ordinal)
            .Replace("      when IssueInvoice\n", "      given caller\n        when IssueInvoice\n", StringComparison.Ordinal);

        // Retain one denial so the renderer has no acceptance expectations to emit.
        var model = invoice_model.Compile(source);
        var module = model.Application.Modules.Single();
        var feature = module.Features.Single();
        var slice = feature.Slices.Single();
        var specification = slice.Specifications[0] with { ThenEvents = [], ThenDenied = true };
        var options = new CratisRenderingOptions("Invoices", "Invoices");
        var context = new SemanticApplicationContext(new(model, SemanticExecutionPlan.Compile(model).Plan!, CratisRendering.CreateProfile(model.Application.Name, options), new(ArtifactRenderScopeKind.Application, model.Application.Id)), options);
        _source = SemanticCommandSpecificationRenderer.Render(specification, context).Content;
    }

    [Fact] void should_preserve_the_explicit_guest_caller() => _source.ShouldContain("ClaimsIdentity([], null)");
    [Fact] void should_not_insert_the_authenticated_default_caller() => _source.ShouldNotContain(SemanticDefaultCaller.Comment);
    [Fact] void should_assert_denial() => _source.ShouldContain("should_be_denied() => _result.IsAuthorized.ShouldBeFalse()");
    [Fact] void should_assert_no_events_were_appended() => _source.ShouldContain("should_not_append_events()");
    [Fact] void should_not_emit_a_policy_resolution_fact() => _source.ShouldNotContain("should_resolve");
    [Fact] void should_not_wrap_in_a_debug_conditional() => _source.ShouldNotContain("#if DEBUG");
}
