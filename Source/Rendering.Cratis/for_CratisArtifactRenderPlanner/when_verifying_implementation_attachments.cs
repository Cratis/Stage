// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Contracts.Semantics;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_verifying_implementation_attachments : Specification
{
    const string Source =
        """
        module Orders
          feature Ordering
            slice StateChange PlaceOrder
              command PlaceOrder
                orderId Uuid identifier
                amount Decimal
                validate csharp
                  ```csharp
                  if (context.Artifact.amount <= 0) yield return "Nothing to order";
                  ```
                produces OrderPlaced
                  orderId = orderId
                  amount = amount
              event OrderPlaced
                orderId Uuid
                amount Decimal
        """;

    string _folder = null!;
    LoadedSemanticModel _loaded = null!;

    void Establish()
    {
        _folder = Path.Combine(Path.GetTempPath(), $"stage-implementation-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_folder);
        File.WriteAllText(Path.Combine(_folder, "Orders.play"), Source);
    }

    async Task Because() => _loaded = await SemanticModelLoader.LoadFromPathAsync(_folder, null, "Orders");

    [Fact] void should_carry_the_inline_body_by_requirement_identity()
    {
        var requirement = _loaded.ImplementationRequirements.Single();
        _loaded.ImplementationContents[requirement.RequirementId].ShouldContain("Nothing to order");
    }

    [Fact] void should_reject_the_missing_body_with_its_requirement_identity()
    {
        var requirement = _loaded.ImplementationRequirements.Single();
        var plan = Render(_loaded.ImplementationContents.Remove(requirement.RequirementId));
        plan.Artifacts.ShouldBeEmpty();
        plan.Diagnostics.Single(diagnostic => diagnostic.Code == "STAGE-ESM-020").Message.ShouldContain(requirement.RequirementId);
    }

    [Fact] void should_reject_a_stale_body_with_its_requirement_identity()
    {
        var requirement = _loaded.ImplementationRequirements.Single();
        var plan = Render(_loaded.ImplementationContents.SetItem(requirement.RequirementId, "return true;"));
        plan.Artifacts.ShouldBeEmpty();
        plan.Diagnostics.Single(diagnostic => diagnostic.Code == "STAGE-ESM-020").Message.ShouldContain(requirement.RequirementId);
    }

    [Fact] async Task should_load_a_file_body_from_the_model_root()
    {
        const string source =
            """
            module Orders
              feature Ordering
                slice StateChange PlaceOrder
                  command PlaceOrder
                    orderId Uuid identifier
                    amount Decimal
                    validate
                      amount rule Positive message "Positive amount required"
                        file Rules/Positive.cs
                    produces OrderPlaced
                      orderId = orderId
                      amount = amount
                  event OrderPlaced
                    orderId Uuid
                    amount Decimal
            """;
        await File.WriteAllTextAsync(Path.Combine(_folder, "Orders.play"), source);
        Directory.CreateDirectory(Path.Combine(_folder, "Rules"));
        await File.WriteAllTextAsync(Path.Combine(_folder, "Rules", "Positive.cs"), "return context.Value > 0;");
        var loaded = await SemanticModelLoader.LoadFromPathAsync(_folder, null, "Orders");
        var requirement = loaded.ImplementationRequirements.Single();
        loaded.ImplementationContents[requirement.RequirementId].ShouldEqual("return context.Value > 0;");
    }

    [Fact] void should_keep_code_validation_rejected_when_the_body_is_verified()
    {
        var plan = Render(_loaded.ImplementationContents);
        plan.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == "STAGE-ESM-005" && diagnostic.Message.Contains("RuleContext.Occurred", StringComparison.Ordinal));
        plan.Artifacts.ShouldBeEmpty();
    }

    ArtifactRenderPlan Render(System.Collections.Immutable.ImmutableDictionary<string, string> contents) =>
        CratisRendering.Plan(
            _loaded.Model,
            _loaded.Plan,
            new(ArtifactRenderScopeKind.Application, _loaded.Model.Application.Id),
            new("Orders", "Orders"),
            _loaded.ImplementationRequirements,
            contents);

    void Destroy() => Directory.Delete(_folder, recursive: true);
}
#endif
