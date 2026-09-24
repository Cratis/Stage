// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_planning_newer_semantic_constructs;

public class with_a_coded_constraint_error : Specification
{
    Contracts.Rendering.ArtifactRenderPlan _plan = null!;

    void Because()
    {
        var source = invoice_model.Source("String", invoice_model.TextSource, invoice_model.OtherTextSource)
            .Replace("      event InvoiceIssued\n", "      constraint UniqueDescription\n        unique description on InvoiceIssued\n      event InvoiceIssued\n", StringComparison.Ordinal)
            .Replace(
                "      specification IssuingSecondInvoice\n",
                """
                  specification ReusingDescription
                    given InvoiceIssued
                      for "invoice-one"
                      description = "First payload"
                    when IssueInvoice
                      description = "First payload"
                      streamReference = "invoice-two"
                    then error "Constraint 'UniqueDescription' is violated: another event source already holds the constrained value."
                  specification IssuingSecondInvoice
            """ + "\n",
                StringComparison.Ordinal);
        var model = invoice_model.Compile(source);
        var application = model.Application with
        {
            Modules = [.. model.Application.Modules.Select(module => module with
            {
                Features = [.. module.Features.Select(feature => feature with
                {
                    Slices = [.. feature.Slices.Select(slice => slice with
                    {
                        Specifications = [.. slice.Specifications.Select(specification => specification.Name == "ReusingDescription"
                            ? specification with { ThenErrors = [new("UniqueDescription", specification.ThenErrors[0].Message)] }
                            : specification)]
                    })]
                })]
            })]
        };
        _plan = invoice_model.Plan(ExecutableSemanticModel.Create(model.LanguageVersion, model.SemanticVersion, application));
    }

    [Fact] void should_admit_the_coded_error() => _plan.Success.ShouldBeTrue();
    [Fact] void should_assert_the_named_violation() => Encoding.UTF8.GetString(_plan.Artifacts.Single(_ => _.RelativePath.EndsWith("when_reusing_description.cs", StringComparison.Ordinal)).Bytes.AsSpan())
        .ShouldContain("ShouldHaveConstraintViolationFor(\"UniqueDescription\")");
}
