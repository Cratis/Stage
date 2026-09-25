// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;
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

    const string FileSource =
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
        await File.WriteAllTextAsync(Path.Combine(_folder, "Orders.play"), FileSource);
        Directory.CreateDirectory(Path.Combine(_folder, "Rules"));
        await File.WriteAllTextAsync(Path.Combine(_folder, "Rules", "Positive.cs"), "return context.Value > 0;");
        var loaded = await SemanticModelLoader.LoadFromPathAsync(_folder, null, "Orders");
        var requirement = loaded.ImplementationRequirements.Single();
        loaded.ImplementationContents[requirement.RequirementId].ShouldEqual("return context.Value > 0;");
        var plan = Render(loaded);
        plan.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == "STAGE-ESM-005");
        plan.Diagnostics.ShouldNotContain(diagnostic => diagnostic.Code == "STAGE-ESM-020");
    }

    [Fact] void should_keep_code_validation_rejected_when_the_body_is_verified()
    {
        var plan = Render(_loaded.ImplementationContents);
        plan.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == "STAGE-ESM-005" &&
            diagnostic.Message.Contains("Arc does not supply RuleContext.Occurred (received-at)", StringComparison.Ordinal) &&
            diagnostic.Message.Contains("does not yet enforce the pure capability", StringComparison.Ordinal) &&
            !diagnostic.Message.Contains("RuleContext.Tenant", StringComparison.Ordinal) &&
            !diagnostic.Message.Contains("RuleContext.CausedBy", StringComparison.Ordinal));
        plan.Artifacts.ShouldBeEmpty();
    }

    [Fact] void should_reject_model_validation_without_any_envelope()
    {
        var plan = CratisRendering.Plan(_loaded.Model, _loaded.Plan, Scope(_loaded), new("Orders", "Orders"), [], []);
        plan.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == "STAGE-ESM-020" &&
            diagnostic.Message.Contains(_loaded.ImplementationRequirements.Single().RequirementId, StringComparison.Ordinal));
        plan.Artifacts.ShouldBeEmpty();
    }

    [Fact] void should_reject_orphan_duplicate_and_default_envelopes()
    {
        Render(_loaded.ImplementationContents.Add("orphan", "body")).Diagnostics.ShouldContain(diagnostic => diagnostic.Code == "STAGE-ESM-020");
        CratisRendering.Plan(
            _loaded.Model,
            _loaded.Plan,
            Scope(_loaded),
            new("Orders", "Orders"),
            [.. _loaded.ImplementationRequirements, _loaded.ImplementationRequirements.Single()],
            _loaded.ImplementationContents)
            .Diagnostics.ShouldContain(diagnostic => diagnostic.Code == "STAGE-ESM-020");
        CratisRendering.Plan(
            _loaded.Model,
            _loaded.Plan,
            Scope(_loaded),
            new("Orders", "Orders"),
            default,
            _loaded.ImplementationContents)
            .Diagnostics.ShouldContain(diagnostic => diagnostic.Code == "STAGE-ESM-020");
    }

    [Fact] void should_accept_init_properties_on_a_plain_request()
    {
        var request = new ArtifactRenderRequest(
            _loaded.Model, _loaded.Plan, CratisRendering.CreateProfile("Orders", new("Orders", "Orders")), Scope(_loaded))
        {
            ImplementationRequirements = _loaded.ImplementationRequirements,
            ImplementationContents = _loaded.ImplementationContents
        };
        var plan = new CratisArtifactRenderPlanner().Plan(request);
        plan.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == "STAGE-ESM-005");
        plan.Diagnostics.ShouldNotContain(diagnostic => diagnostic.Code == "STAGE-ESM-020");
    }

    [Fact] async Task should_reject_a_file_edited_after_loading()
    {
        var loaded = await LoadFileBody("return context.Value > 0;");
        var requirement = loaded.ImplementationRequirements.Single();
        await File.WriteAllTextAsync(Path.Combine(_folder, "Rules", "Positive.cs"), "return false;");
        var reloaded = await SemanticModelLoader.LoadFromPathAsync(_folder, null, "Orders");
        Render(loaded, reloaded.ImplementationContents).Diagnostics.ShouldContain(diagnostic =>
            diagnostic.Code == "STAGE-ESM-020" && diagnostic.Message.Contains(requirement.RequirementId, StringComparison.Ordinal));
    }

    [Fact] async Task should_report_missing_file_and_its_loader_reason()
    {
        var loaded = await LoadFileBody(null);
        loaded.ImplementationRequirements.Single().AttachmentResolution.ShouldEqual(SemanticAttachmentResolution.UnresolvedFile);
        var code = loaded.AttachmentDiagnostics.Single().Code;
        code.ShouldEqual("PLAY0432");
        Render(loaded).Diagnostics.ShouldContain(diagnostic => diagnostic.Code == "STAGE-ESM-020" && diagnostic.Message.Contains(code, StringComparison.Ordinal));
    }

    [Fact] async Task should_refuse_an_out_of_root_file()
    {
        await File.WriteAllTextAsync(Path.Combine(_folder, "Orders.play"), FileSource.Replace("Rules/Positive.cs", "../x.cs", StringComparison.Ordinal));
        var loaded = await SemanticModelLoader.LoadFromPathAsync(_folder, null, "Orders");
        loaded.ImplementationRequirements.Single().AttachmentResolution.ShouldEqual(SemanticAttachmentResolution.UnresolvedFile);
        Render(loaded).Diagnostics.ShouldContain(diagnostic => diagnostic.Code == "STAGE-ESM-020" && diagnostic.Message.Contains("PLAY0430", StringComparison.Ordinal));
    }

    [Fact] async Task should_refuse_a_symlinked_file()
    {
        Directory.CreateDirectory(Path.Combine(_folder, "Rules"));
        await File.WriteAllTextAsync(Path.Combine(_folder, "real.cs"), "return true;");
        File.CreateSymbolicLink(Path.Combine(_folder, "Rules", "Positive.cs"), Path.Combine(_folder, "real.cs"));
        await File.WriteAllTextAsync(Path.Combine(_folder, "Orders.play"), FileSource);
        var loaded = await SemanticModelLoader.LoadFromPathAsync(_folder, null, "Orders");
        loaded.ImplementationRequirements.Single().AttachmentResolution.ShouldEqual(SemanticAttachmentResolution.UnresolvedFile);
        Render(loaded).Diagnostics.ShouldContain(diagnostic => diagnostic.Code == "STAGE-ESM-020" && diagnostic.Message.Contains("PLAY0431", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    async Task should_hash_exact_file_text_including_crlf_and_bom(bool bom)
    {
        var text = (bom ? "\uFEFF" : string.Empty) + "return true;\r\nreturn false;\r\n";
        var loaded = await LoadFileBody(text);
        var requirement = loaded.ImplementationRequirements.Single();
        loaded.ImplementationContents[requirement.RequirementId].ShouldEqual(text);
        requirement.ContentHash.ShouldEqual(Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(text))).ToLowerInvariant());
        Render(loaded).Diagnostics.ShouldContain(diagnostic => diagnostic.Code == "STAGE-ESM-005");
    }

    [Fact] async Task should_list_concept_validation_requirement_ids_in_005()
    {
        const string source = "concept Label : String\n  validate\n    ```csharp\n    yield return \"Invalid label\";\n    ```\n" + Source;
        await File.WriteAllTextAsync(Path.Combine(_folder, "Orders.play"), source);
        var loaded = await SemanticModelLoader.LoadFromPathAsync(_folder, null, "Orders");
        Render(loaded).Diagnostics.ShouldContain(diagnostic => diagnostic.Code == "STAGE-ESM-005" &&
            diagnostic.Message.Contains(loaded.ImplementationRequirements.Single(requirement => requirement.Role == SemanticImplementationRole.ConceptValidation).RequirementId, StringComparison.Ordinal));
    }

    [Fact] async Task should_reject_a_verified_reducer_with_019()
    {
        const string source = """
            module Billing
              feature Accounts
                slice StateView Balance
                  event AmountDeposited
                    amount Decimal
                  readmodel AccountBalance
                    balance Decimal
                    id Uuid
                  query BalanceById => AccountBalance?
                    by id Uuid
                  reducer Balance => AccountBalance
                    on AmountDeposited
                      csharp
                        ```
                        return new(context.Event.amount);
                        ```
            """;
        await File.WriteAllTextAsync(Path.Combine(_folder, "Orders.play"), source);
        var loaded = await SemanticModelLoader.LoadFromPathAsync(_folder, null, "Orders");
        var plan = Render(loaded);
        plan.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == "STAGE-ESM-019");
        plan.Diagnostics.ShouldNotContain(diagnostic => diagnostic.Code == "STAGE-ESM-020");
    }

    async Task<LoadedSemanticModel> LoadFileBody(string? content)
    {
        await File.WriteAllTextAsync(Path.Combine(_folder, "Orders.play"), FileSource);
        if (content is not null)
        {
            Directory.CreateDirectory(Path.Combine(_folder, "Rules"));
            await File.WriteAllTextAsync(Path.Combine(_folder, "Rules", "Positive.cs"), content);
        }

        return await SemanticModelLoader.LoadFromPathAsync(_folder, null, "Orders");
    }

    static ArtifactRenderScope Scope(LoadedSemanticModel loaded) => new(ArtifactRenderScopeKind.Application, loaded.Model.Application.Id);

    static ArtifactRenderPlan Render(LoadedSemanticModel loaded, ImmutableDictionary<string, string>? contents = null) =>
        CratisRendering.Plan(
            loaded.Model,
            loaded.Plan,
            Scope(loaded),
            new("Orders", "Orders"),
            loaded.ImplementationRequirements,
            contents ?? loaded.ImplementationContents,
            loaded.AttachmentDiagnostics);

    ArtifactRenderPlan Render(ImmutableDictionary<string, string> contents) =>
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
