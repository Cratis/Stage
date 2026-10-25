// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;
public class when_omitting_namespaces_beside_generated_declarations
{
    const string Independent = """
        module Independent
          authorize Access
          feature Receipts
            slice StateChange Record
              command RecordReceipt
                authorize Owns
                invoiceId InvoiceId identifier
                description String
                produces ReceiptRecorded
                  for invoiceId
                  invoiceId = invoiceId
                  description = description
              event ReceiptRecorded
                invoiceId InvoiceId
                description String
        """;

    [Theory]
    [InlineData("Registration")]
    [InlineData("PolicyValues")]
    public void should_omit_the_colliding_namespace_and_retain_independent_authorized_output(string featureName)
    {
        var source = when_rendering_portable_authorization.Source
            .Replace("module Billing", "module GeneratedPolicies", StringComparison.Ordinal)
            .Replace("feature Invoicing", $"feature {featureName}", StringComparison.Ordinal);
        var model = invoice_model.Compile(source + "\n" + Independent);
        var plan = invoice_model.Plan(model);
        var refused = model.Application.Modules.Single(module => module.Name == "GeneratedPolicies").Features.Single();
        var retained = model.Application.Modules.Single(module => module.Name == "Independent").Features.Single().Slices.Single();
        var collision = Assert.Single(plan.Diagnostics.Where(diagnostic => diagnostic.Code == "STAGE-ESM-012" && diagnostic.Artifact == refused.Id));
        rendering_refusals.Warning(plan, collision.Code, refused.Id, collision.Message);
        rendering_refusals.Omitted(plan, refused.Id);
        foreach (var slice in refused.Slices)
        {
            rendering_refusals.Omitted(plan, slice.Id);
            foreach (var operation in slice.Commands.Select(command => command.Id).Concat(slice.Queries.Select(query => query.Id)))
            {
                rendering_refusals.Omitted(plan, operation);
            }
        }
        rendering_refusals.Retained(plan, retained.Commands.Single().Id);
        rendering_refusals.Retained(plan, retained.Events.Single().Id);
        Assert.Contains(plan.Artifacts, artifact => artifact.RelativePath.Contains("GeneratedPolicies/", StringComparison.Ordinal) &&
            artifact.Sources.Contains(retained.Commands.Single().Id));
        Assert.Contains(plan.Artifacts, artifact => artifact.RelativePath.EndsWith("GeneratedPolicies/Policies.cs", StringComparison.Ordinal));
        rendering_refusals.Compiles(plan);
    }
}
#endif
