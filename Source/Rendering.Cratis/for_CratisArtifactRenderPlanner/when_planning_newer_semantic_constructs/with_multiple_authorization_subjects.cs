// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_planning_newer_semantic_constructs;

public class with_multiple_authorization_subjects : given.an_invoice_model
{
    void Because()
    {
        var source = Invoices.Replace("        produces InvoiceIssued\n", "        authorize Staff\n        produces InvoiceIssued\n", StringComparison.Ordinal);
        _model = invoice_model.Compile(
            "policy Staff\n  require authenticated\n" + source[..source.IndexOf("      specification", StringComparison.Ordinal)]);
        var module = _model.Application.Modules.Single();
        var feature = module.Features.Single();
        var slice = feature.Slices.Single();
        var command = slice.Commands.Single();
        var properties = command.Properties.Select(property => property.Name == "description" ? property with { IsIdentifier = true } : property);
        var application = _model.Application with
        {
            Modules = [module with { Features = [feature with { Slices = [slice with { Commands = [command with { Properties = [.. properties] }] }] }] }]
        };
        _model = ExecutableSemanticModel.Create(_model.LanguageVersion, _model.SemanticVersion, application);
        _plan = invoice_model.Plan(_model);
    }

    [Fact] void should_reject_an_ambiguous_subject() => ErrorCodes.ShouldContain("STAGE-ESM-015");
    [Fact] void should_plan_no_artifacts() => _plan.Artifacts.ShouldBeEmpty();
}
