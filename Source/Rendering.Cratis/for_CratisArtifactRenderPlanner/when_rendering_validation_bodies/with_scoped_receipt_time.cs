// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_rendering_validation_bodies;

public class with_scoped_receipt_time : Specification
{
    [Fact]
    public void should_render_an_unused_concept_receipt_helper_in_a_slice_scope()
    {
        var loaded = validation_body_model.Load(with_an_unused_receipt_time_concept.Source);
        var slice = loaded.Model.Application.Modules.Single().Features.Single().Slices.Single();
        var plan = CratisRendering.Plan(loaded.Model, loaded.Plan, new(ArtifactRenderScopeKind.Slice, slice.Id), new("InvoiceApp", "Invoices"), loaded.ImplementationRequirements, loaded.ImplementationContents, loaded.AttachmentDiagnostics, loaded.TypedContextDescriptors);
        Assert.True(plan.Success, opaque_policy_model.Errors(plan));
        Assert.Single(plan.Artifacts, artifact => artifact.RelativePath.EndsWith("CommandReceiptTime.cs", StringComparison.Ordinal));
        opaque_policy_model.Compile(plan);
    }

    [Fact]
    public void should_not_reserve_or_emit_a_helper_for_an_out_of_scope_command_rule()
    {
        var source = validation_body_model.Command("return (DateTimeOffset)context.Value > context.Occurred;")
            .Replace("amount Decimal", "amount DateTime", StringComparison.Ordinal) + "\n" + """
            module GeneratedCommands
              feature CommandReceiptTime
                slice StateChange Clean
                  command Register
                    orderId OrderId identifier
                    produces Registered
                      for orderId
                  event Registered
            """;
        var loaded = validation_body_model.Load(source);
        var slice = loaded.Model.Application.Modules.Single(module => module.Name == "GeneratedCommands").Features.Single().Slices.Single();
        var plan = CratisRendering.Plan(loaded.Model, loaded.Plan, new(ArtifactRenderScopeKind.Slice, slice.Id), new("InvoiceApp", "Invoices"), loaded.ImplementationRequirements, loaded.ImplementationContents, loaded.AttachmentDiagnostics, loaded.TypedContextDescriptors);
        Assert.True(plan.Success, opaque_policy_model.Errors(plan));
        Assert.DoesNotContain(plan.Artifacts, artifact => artifact.RelativePath.EndsWith("CommandReceiptTime.cs", StringComparison.Ordinal));
        opaque_policy_model.Compile(plan);
    }

    [Fact]
    public void should_emit_only_one_helper_when_the_command_mapping_already_needs_it()
    {
        var source = validation_body_model.Command("return (DateTimeOffset)context.Value > context.Occurred;")
            .Replace("amount Decimal", "amount DateTime", StringComparison.Ordinal)
            .Replace("amount = amount", "amount = $context.occurred", StringComparison.Ordinal);
        var plan = opaque_policy_model.Plan(validation_body_model.Load(source));
        Assert.True(plan.Success, opaque_policy_model.Errors(plan));
        Assert.Single(plan.Artifacts, artifact => artifact.RelativePath.EndsWith("CommandReceiptTime.cs", StringComparison.Ordinal));
        opaque_policy_model.Compile(plan);
    }
}
#endif
