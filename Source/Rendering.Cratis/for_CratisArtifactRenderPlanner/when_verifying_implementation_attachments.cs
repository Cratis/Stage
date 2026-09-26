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

    [Fact] async Task should_emit_no_typed_contexts_for_a_slice_when_other_slices_have_a_reducer_rule_and_policy()
    {
        const string policy = """
            policy Access
              ```csharp
              return true;
              ```
            """;
        var source = policy + "\n" + FileSource.Replace("    slice StateChange PlaceOrder", "    slice StateChange Healthy\n      command CreateHealthy\n        id Uuid identifier\n        produces HealthyCreated\n          for id\n          id = id\n      event HealthyCreated\n        id Uuid\n    slice StateView Totals\n      readmodel Total\n        id Uuid\n        amount Decimal\n      query ById => Total?\n        by id Uuid\n      reducer Fold => Total\n        on OrderPlaced\n          file Reducers/OrderPlaced.cs\n    slice StateChange PlaceOrder", StringComparison.Ordinal)
            .Replace("      command PlaceOrder\n", "      command PlaceOrder\n        authorize Access\n", StringComparison.Ordinal);
        await File.WriteAllTextAsync(Path.Combine(_folder, "Orders.play"), source);
        Directory.CreateDirectory(Path.Combine(_folder, "Rules"));
        await File.WriteAllTextAsync(Path.Combine(_folder, "Rules", "Positive.cs"), "return context.Value > 0;");
        Directory.CreateDirectory(Path.Combine(_folder, "Reducers"));
        await File.WriteAllTextAsync(Path.Combine(_folder, "Reducers", "OrderPlaced.cs"), "return state;");
        var loaded = await SemanticModelLoader.LoadFromPathAsync(_folder, null, "Orders");
        Assert.Contains(loaded.TypedContextDescriptors, descriptor => descriptor.Role == SemanticImplementationRole.RulePredicate);
        Assert.Contains(loaded.TypedContextDescriptors, descriptor => descriptor.Role == SemanticImplementationRole.PolicyPredicate);
        Assert.Contains(loaded.TypedContextDescriptors, descriptor => descriptor.Role == SemanticImplementationRole.ReducerTransition);
        var healthy = loaded.Model.Application.Modules.Single().Features.Single().Slices.Single(slice => slice.Name == "Healthy");
        var request = new ArtifactRenderRequest(
            loaded.Model,
            loaded.Plan,
            CratisRendering.CreateProfile("Orders", new("Orders", "Orders")),
            new(ArtifactRenderScopeKind.Slice, healthy.Id))
        {
            ImplementationRequirements = loaded.ImplementationRequirements,
            ImplementationContents = loaded.ImplementationContents,
            TypedContextDescriptors = loaded.TypedContextDescriptors
        };
        var plan = new CratisArtifactRenderPlanner().Plan(request);
        Assert.True(plan.Success, string.Join(Environment.NewLine, plan.Diagnostics));
        Assert.DoesNotContain(plan.Artifacts, artifact => artifact.RelativePath.StartsWith("TypedContexts/", StringComparison.Ordinal));
    }

    [Fact] async Task should_require_a_policy_descriptor_for_each_authorized_use_site()
    {
        const string policySource = """
            policy Access
              ```csharp
              return true;
              ```
            """;
        var modelSource = policySource + "\n" + Source.Replace("    slice StateChange PlaceOrder", "    slice StateChange Healthy\n      command CreateHealthy\n        id Uuid identifier\n        authorize Access\n        produces HealthyCreated\n          for id\n          id = id\n      event HealthyCreated\n        id Uuid\n    slice StateChange PlaceOrder", StringComparison.Ordinal)
            .Replace("      command PlaceOrder\n", "      command PlaceOrder\n        authorize Access\n", StringComparison.Ordinal);
        await File.WriteAllTextAsync(Path.Combine(_folder, "Orders.play"), modelSource);
        var loaded = await SemanticModelLoader.LoadFromPathAsync(_folder, null, "Orders");
        var policies = loaded.TypedContextDescriptors.Where(descriptor => descriptor.Role == SemanticImplementationRole.PolicyPredicate).ToArray();
        Assert.Equal(2, policies.Length);
        var request = new ArtifactRenderRequest(
            loaded.Model,
            loaded.Plan,
            CratisRendering.CreateProfile("Orders", new("Orders", "Orders")),
            new(ArtifactRenderScopeKind.Application, loaded.Model.Application.Id))
        {
            ImplementationRequirements = loaded.ImplementationRequirements,
            ImplementationContents = loaded.ImplementationContents,
            TypedContextDescriptors = [.. loaded.TypedContextDescriptors.Where(descriptor => descriptor != policies[1])]
        };
        Assert.Contains(new CratisArtifactRenderPlanner().Plan(request).Diagnostics, diagnostic =>
            diagnostic.Code == "STAGE-ESM-021" && diagnostic.Message.Contains("use site", StringComparison.Ordinal));
    }

    [Fact] void should_carry_the_inline_body_by_requirement_identity()
    {
        var requirement = _loaded.ImplementationRequirements.Single();
        _loaded.ImplementationContents[requirement.RequirementId].ShouldContain("Nothing to order");
    }

    [Fact] void should_carry_ready_descriptors_with_the_same_model_revision()
    {
        var descriptor = _loaded.TypedContextDescriptors.Single();
        descriptor.RequirementId.ShouldEqual(_loaded.ImplementationRequirements.Single().RequirementId);
        descriptor.IsWrapperReady.ShouldBeTrue();
        descriptor.ModelRevision.ShouldEqual(_loaded.Model.Revision);
        CratisRendering.Plan(
            _loaded.Model,
            _loaded.Plan,
            Scope(_loaded),
            new("Orders", "Orders"),
            _loaded.ImplementationRequirements,
            _loaded.ImplementationContents,
            _loaded.AttachmentDiagnostics,
            _loaded.TypedContextDescriptors).Diagnostics.ShouldContain(diagnostic => diagnostic.Code == "STAGE-ESM-005");
    }

    [Fact] void should_reject_a_descriptor_from_another_model_revision()
    {
        var model = ExecutableSemanticModel.Create(
            _loaded.Model.LanguageVersion,
            _loaded.Model.SemanticVersion,
            _loaded.Model.Application with { Name = "DifferentOrders" });
        var request = new ArtifactRenderRequest(
            model,
            global::Cratis.Screenplay.Semantics.Execution.SemanticExecutionPlan.Compile(model).Plan!,
            CratisRendering.CreateProfile("DifferentOrders", new("Orders", "Orders")),
            new(ArtifactRenderScopeKind.Application, model.Application.Id))
        {
            ImplementationRequirements = _loaded.ImplementationRequirements,
            ImplementationContents = _loaded.ImplementationContents,
            TypedContextDescriptors = _loaded.TypedContextDescriptors
        };
        Assert.Contains(new CratisArtifactRenderPlanner().Plan(request).Diagnostics, diagnostic =>
            diagnostic.Code == "STAGE-ESM-021" && diagnostic.Message.Contains("revision", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void should_reject_a_mismatched_role_or_context_version(bool role)
    {
        var descriptor = _loaded.TypedContextDescriptors.Single();
        var changed = role ? descriptor with { Role = SemanticImplementationRole.RulePredicate }
            : descriptor with { ContextVersion = 2 };
        var plan = CratisRendering.Plan(
            _loaded.Model,
            _loaded.Plan,
            Scope(_loaded),
            new("Orders", "Orders"),
            _loaded.ImplementationRequirements,
            _loaded.ImplementationContents,
            _loaded.AttachmentDiagnostics,
            [changed]);
        Assert.Contains(plan.Diagnostics, diagnostic => diagnostic.Code == "STAGE-ESM-021" &&
            diagnostic.Message.Contains("role, context version", StringComparison.Ordinal));
    }

    [Fact] void should_reject_a_descriptor_with_no_matching_compiler_requirement()
    {
        var descriptor = _loaded.TypedContextDescriptors.Single() with { RequirementId = "unrelated" };
        CratisRendering.Plan(
            _loaded.Model,
            _loaded.Plan,
            Scope(_loaded),
            new("Orders", "Orders"),
            _loaded.ImplementationRequirements,
            _loaded.ImplementationContents,
            _loaded.AttachmentDiagnostics,
            [descriptor]).Diagnostics.ShouldContain(diagnostic => diagnostic.Code == "STAGE-ESM-021");
    }

    [Fact] void should_collect_target_and_descriptor_diagnostics_together()
    {
        var descriptor = _loaded.TypedContextDescriptors.Single();
        var member = descriptor.Members[0];
        var malformed = descriptor with
        {
            Members = descriptor.Members.SetItem(0, member with
            {
                Type = new(SemanticContextTypeKinds.Runtime, null, null, "UnsupportedToken")
            })
        };
        var plan = CratisRendering.Plan(
            _loaded.Model,
            _loaded.Plan,
            Scope(_loaded),
            new("Orders", "Orders"),
            _loaded.ImplementationRequirements,
            _loaded.ImplementationContents,
            _loaded.AttachmentDiagnostics,
            [malformed]);
        Assert.Contains(plan.Diagnostics, diagnostic => diagnostic.Code == "STAGE-ESM-005");
        Assert.Contains(plan.Diagnostics, diagnostic => diagnostic.Code == "STAGE-ESM-021");
    }

    [Fact] void should_diagnose_an_unknown_runtime_token_without_falling_back_to_dynamic()
    {
        var descriptor = _loaded.TypedContextDescriptors.Single();
        var member = descriptor.Members[0];
        var unknown = descriptor with
        {
            Members = descriptor.Members.SetItem(0, member with
            {
                Type = new(SemanticContextTypeKinds.Runtime, null, null, "UnsupportedToken")
            })
        };
        var plan = CratisRendering.Plan(
            _loaded.Model,
            _loaded.Plan,
            Scope(_loaded),
            new("Orders", "Orders"),
            _loaded.ImplementationRequirements,
            _loaded.ImplementationContents,
            _loaded.AttachmentDiagnostics,
            [unknown]);
        plan.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == "STAGE-ESM-021" &&
            diagnostic.Message.Contains("UnsupportedToken", StringComparison.Ordinal));
        plan.Artifacts.ShouldBeEmpty();
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
