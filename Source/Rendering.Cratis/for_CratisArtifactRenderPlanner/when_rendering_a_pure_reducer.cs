// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Reflection;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Contracts.Semantics;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using Cratis.Stage.Rendering.Cratis.Naming;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_rendering_a_pure_reducer
{
    internal const string Source = """
        module Orders
          feature Ordering
            slice StateChange PlaceOrder
              command PlaceOrder
                id Uuid identifier
                amount Decimal
                produces OrderPlaced
                  for id
                  id = id
                  amount = amount
              event OrderPlaced
                id Uuid
                amount Decimal
            slice StateView Totals
              readmodel Total
                id Uuid
                amount Decimal
              query ById => Total?
                by id Uuid
              reducer Fold => Total
                on OrderPlaced
                  ```csharp
                  return new Total(Guid.Parse("00000000-0000-0000-0000-000000000001"), context.Event.Amount);
                  ```
        """;

    [Fact]
    public async Task should_reject_a_reducer_outside_a_state_view()
    {
        var source = Source.Replace("      reducer Fold => Total\n        on OrderPlaced\n          ```csharp\n          return new Total(Guid.Parse(\"00000000-0000-0000-0000-000000000001\"), context.Event.Amount);\n          ```", string.Empty, StringComparison.Ordinal)
            .Replace("      event OrderPlaced\n        id Uuid\n        amount Decimal", "      event OrderPlaced\n        id Uuid\n        amount Decimal\n      reducer Fold => Total\n        on OrderPlaced\n          ```csharp\n          return new Total(Guid.Empty, context.Event.Amount);\n          ```", StringComparison.Ordinal);
        var loaded = await Load(source);
        Assert.Contains(Plan(loaded).Diagnostics, diagnostic => diagnostic.Code == "STAGE-ESM-019" && diagnostic.Message.Contains("StateView", StringComparison.Ordinal));
    }

    [Fact]
    public async Task should_reject_a_reducer_whose_read_model_is_in_another_slice()
    {
        var source = Source.Replace("    slice StateView Totals\n      readmodel Total", "    slice StateView Models\n      readmodel Total", StringComparison.Ordinal)
            .Replace("      reducer Fold => Total", "    slice StateView Totals\n      reducer Fold => Total", StringComparison.Ordinal)
            .Replace("Guid.Parse(\"00000000-0000-0000-0000-000000000001\")", "Guid.Empty", StringComparison.Ordinal);
        var loaded = await Load(source);
        var plan = Plan(loaded);
        Assert.Contains(plan.Diagnostics, diagnostic => diagnostic.Code == "STAGE-ESM-019" && diagnostic.Message.Contains("same slice", StringComparison.Ordinal));
        var models = loaded.Model.Application.Modules.Single().Features.Single().Slices.Single(slice => slice.ReadModels.Length > 0);
        Assert.Contains(Plan(loaded, new(ArtifactRenderScopeKind.Slice, models.Id)).Diagnostics,
            diagnostic => diagnostic.Code == "STAGE-ESM-007");
    }

    [Fact]
    public void should_retain_same_slice_projection_admission()
    {
        const string moved = "    slice StateView Other\n      readmodel OtherSummary\n        invoiceId InvoiceId\n        description String\n      query OtherById => OtherSummary?\n        by invoiceId InvoiceId\n      projection OtherSummaryProjection => OtherSummary\n        from InvoiceIssued key invoiceId\n          invoiceId = invoiceId\n          description = description\n      projection InvoiceSummaryProjection => InvoiceSummary";
        var source = when_rendering_portable_authorization.Source.Replace("      projection InvoiceSummaryProjection => InvoiceSummary", moved, StringComparison.Ordinal);
        Assert.Contains("slice StateView Other", source, StringComparison.Ordinal);
        var model = invoice_model.Compile(source);
        var plan = invoice_model.Plan(model);
        Assert.Contains(plan.Diagnostics, diagnostic => diagnostic.Code == "STAGE-ESM-007");
        var other = model.Application.Modules.Single().Features.Single().Slices.Single(slice => slice.Name == "Other");
        Assert.Contains(invoice_model.Plan(model, new(ArtifactRenderScopeKind.Slice, other.Id)).Diagnostics,
            diagnostic => diagnostic.Code == "STAGE-ESM-007");
    }

    [Fact]
    public async Task should_reject_an_observed_event_declared_in_the_state_view_slice()
    {
        var source = Source.Replace("      event OrderPlaced\n        id Uuid\n        amount Decimal\n", string.Empty, StringComparison.Ordinal)
            .Replace("    slice StateView Totals\n", "    slice StateView Totals\n      event OrderPlaced\n        id Uuid\n        amount Decimal\n", StringComparison.Ordinal)
            .Replace("Guid.Parse(\"00000000-0000-0000-0000-000000000001\")", "context.Event.Id", StringComparison.Ordinal);
        var plan = Plan(await Load(source));
        Assert.Contains(plan.Diagnostics, diagnostic => diagnostic.Code == "STAGE-ESM-019" && diagnostic.Message.Contains("cannot render an event", StringComparison.Ordinal));
    }

    [Fact]
    public async Task should_reject_an_observed_event_declared_in_a_sibling_state_view_slice()
    {
        var source = Source.Replace("      event OrderPlaced\n        id Uuid\n        amount Decimal\n", string.Empty, StringComparison.Ordinal)
            .Replace("    slice StateView Totals\n", "    slice StateView Other\n      event OrderPlaced\n        id Uuid\n        amount Decimal\n    slice StateView Totals\n", StringComparison.Ordinal)
            .Replace("Guid.Parse(\"00000000-0000-0000-0000-000000000001\")", "context.Event.Id", StringComparison.Ordinal);
        var plan = Plan(await Load(source));
        Assert.Contains(plan.Diagnostics, diagnostic => diagnostic.Code == "STAGE-ESM-019" && diagnostic.Message.Contains("cannot render an event", StringComparison.Ordinal));
    }

    [Fact]
    public async Task should_reject_a_reducer_class_colliding_with_a_read_model()
    {
        var source = Source.Replace("reducer Fold => Total", "reducer Total => Total", StringComparison.Ordinal)
            .Replace("Guid.Parse(\"00000000-0000-0000-0000-000000000001\")", "Guid.Empty", StringComparison.Ordinal);
        var plan = Plan(await Load(source));
        Assert.Contains(plan.Diagnostics, diagnostic => diagnostic.Code == "STAGE-ESM-019" && diagnostic.Message.Contains("collides", StringComparison.Ordinal));
    }

    [Fact]
    public async Task should_reject_a_reducer_file_colliding_with_the_slice_file()
    {
        var source = Source.Replace("reducer Fold => Total", "reducer Totals => Total", StringComparison.Ordinal)
            .Replace("Guid.Parse(\"00000000-0000-0000-0000-000000000001\")", "Guid.Empty", StringComparison.Ordinal);
        var plan = Plan(await Load(source));
        Assert.Contains(plan.Diagnostics, diagnostic => diagnostic.Code == "STAGE-ESM-019" && diagnostic.Message.Contains("collides", StringComparison.Ordinal));
    }

    [Fact]
    public async Task should_reject_a_sibling_model_shadowing_an_audited_bcl_type()
    {
        var source = Source.Replace("      readmodel Total", "      readmodel Math\n        id Uuid\n        amount Decimal\n      query MathById => Math?\n        by id Uuid\n      reducer MathFold => Math\n        on OrderPlaced\n          ```csharp\n          return new Math(context.Event.Id, context.Event.Amount);\n          ```\n      readmodel Total", StringComparison.Ordinal)
            .Replace("return new Total(Guid.Parse(\"00000000-0000-0000-0000-000000000001\"), context.Event.Amount);", "return new Total(context.Event.Id, Math.Abs(context.Event.Amount));", StringComparison.Ordinal);
        var plan = Plan(await Load(source));
        Assert.Contains(plan.Diagnostics, diagnostic => diagnostic.Code == "STAGE-ESM-022" && diagnostic.Message.Contains("Math", StringComparison.Ordinal));
    }

    [Fact]
    public async Task should_refuse_a_sibling_primitive_clr_name_before_the_real_reducer_rebinds()
    {
        var source = Source.Replace("      readmodel Total", "      readmodel Int32\n        id Uuid\n        amount Decimal\n      query Int32ById => Int32?\n        by id Uuid\n      reducer Int32Fold => Int32\n        on OrderPlaced\n          ```csharp\n          return new Int32(context.Event.Id, context.Event.Amount);\n          ```\n      readmodel Total", StringComparison.Ordinal)
            .Replace("return new Total(Guid.Parse(\"00000000-0000-0000-0000-000000000001\"), context.Event.Amount);", "Int32 count = 1; return new Total(context.Event.Id, count);", StringComparison.Ordinal);
        var plan = Plan(await Load(source));
        Assert.Contains(plan.Diagnostics, diagnostic => diagnostic.Code == "STAGE-ESM-022" && diagnostic.Message.Contains("Int32", StringComparison.Ordinal));
        Assert.Empty(plan.Artifacts);

        var baseline = Plan(await Load(Source.Replace(
            "return new Total(Guid.Parse(\"00000000-0000-0000-0000-000000000001\"), context.Event.Amount);",
            "Int32 count = 1; return new Total(context.Event.Id, count);",
            StringComparison.Ordinal)));
        Assert.True(baseline.Success, string.Join(Environment.NewLine, baseline.Diagnostics));
        var files = baseline.Artifacts.Where(artifact => artifact.RelativePath.EndsWith(".cs", StringComparison.Ordinal) && artifact.RelativePath != "Program.cs")
            .Select(artifact => new RenderedFile(artifact.RelativePath, System.Text.Encoding.UTF8.GetString(artifact.Bytes.AsSpan())))
            .Append(new RenderedFile("Int32.cs", "namespace Projects.Orders.Ordering.Totals; public record Int32(System.Guid Id, decimal Amount);"));
        Assert.NotEmpty(RenderedOutput.Errors(files));
    }

    [Fact]
    public async Task should_refuse_a_constraint_from_an_imported_event_slice_shadowing_math()
    {
        var source = Source.Replace("      event OrderPlaced\n        id Uuid\n        amount Decimal", "      event OrderPlaced\n        id Uuid\n        amount Decimal\n      constraint Math\n        unique event OrderPlaced", StringComparison.Ordinal)
            .Replace("return new Total(Guid.Parse(\"00000000-0000-0000-0000-000000000001\"), context.Event.Amount);", "return new Total(context.Event.Id, Math.Abs(context.Event.Amount));", StringComparison.Ordinal);
        var plan = Plan(await Load(source));
        Assert.Contains(plan.Diagnostics, diagnostic => diagnostic.Code == "STAGE-ESM-022" && diagnostic.Message.Contains("Math", StringComparison.Ordinal));
        Assert.Empty(plan.Artifacts);
    }

    [Theory]
    [InlineData("EventContext")]
    [InlineData("ReducerContextValues")]
    public async Task should_reject_reducer_names_shadowing_runtime_inputs(string name)
    {
        var source = Source.Replace("reducer Fold => Total", $"reducer {name} => Total", StringComparison.Ordinal)
            .Replace("Guid.Parse(\"00000000-0000-0000-0000-000000000001\")", "context.Event.Id", StringComparison.Ordinal);
        var plan = Plan(await Load(source));
        Assert.Contains(plan.Diagnostics, diagnostic => diagnostic.Code == "STAGE-ESM-022" && diagnostic.Message.Contains(name, StringComparison.Ordinal));
    }

    [Fact]
    public async Task should_reject_an_imported_event_shadowing_math()
    {
        var source = Source.Replace("      event OrderPlaced\n        id Uuid\n        amount Decimal", "      event OrderPlaced\n        id Uuid\n        amount Decimal\n      event Math\n        id Uuid\n        amount Decimal", StringComparison.Ordinal)
            .Replace("on OrderPlaced", "on Math", StringComparison.Ordinal)
            .Replace("Guid.Parse(\"00000000-0000-0000-0000-000000000001\")", "context.Event.Id", StringComparison.Ordinal)
            .Replace("context.Event.Amount);", "Math.Abs(context.Event.Amount));", StringComparison.Ordinal);
        var plan = Plan(await Load(source));
        Assert.Contains(plan.Diagnostics, diagnostic => diagnostic.Code == "STAGE-ESM-022" && diagnostic.Message.Contains("Math", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("    slice StateChange PlaceOrder", "    slice StateChange Math")]
    [InlineData("  feature Ordering", "  feature Math")]
    public async Task should_refuse_namespace_shadows_before_emitting_an_uncompilable_reducer(string original, string replacement)
    {
        var source = Source.Replace(original, replacement, StringComparison.Ordinal)
            .Replace("Guid.Parse(\"00000000-0000-0000-0000-000000000001\")", "context.Event.Id", StringComparison.Ordinal)
            .Replace("context.Event.Amount);", "Math.Abs(context.Event.Amount));", StringComparison.Ordinal);
        var plan = Plan(await Load(source));
        if (plan.Success)
        {
            var files = plan.Artifacts.Where(artifact => artifact.RelativePath.EndsWith(".cs", StringComparison.Ordinal) && artifact.RelativePath != "Program.cs")
                .Select(artifact => new RenderedFile(artifact.RelativePath, System.Text.Encoding.UTF8.GetString(artifact.Bytes.AsSpan())));
            var errors = RenderedOutput.CreateCompilation(files).GetDiagnostics().Where(diagnostic => diagnostic.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error);
            Assert.Fail($"Admitted a namespace shadow; rendered compilation: {string.Join("; ", errors)}");
        }

        Assert.Contains(plan.Diagnostics, diagnostic => diagnostic.Code == "STAGE-ESM-022" && diagnostic.Message.Contains("Math", StringComparison.Ordinal));
    }

    [Fact]
    public async Task should_detect_the_namespace_shadow_in_rendered_compilation()
    {
        var source = Source.Replace("Guid.Parse(\"00000000-0000-0000-0000-000000000001\")", "context.Event.Id", StringComparison.Ordinal)
            .Replace("context.Event.Amount);", "Math.Abs(context.Event.Amount));", StringComparison.Ordinal);
        var plan = Plan(await Load(source));
        Assert.True(plan.Success, string.Join("; ", plan.Diagnostics));
        var files = plan.Artifacts.Where(artifact => artifact.RelativePath.EndsWith(".cs", StringComparison.Ordinal) && artifact.RelativePath != "Program.cs")
            .Select(artifact => new RenderedFile(artifact.RelativePath, System.Text.Encoding.UTF8.GetString(artifact.Bytes.AsSpan())))
            .Append(new RenderedFile("Math.cs", "namespace Projects.Orders.Ordering.Math { }"));
        var errors = RenderedOutput.CreateCompilation(files).GetDiagnostics()
            .Where(diagnostic => diagnostic.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error).ToArray();
        Assert.Contains(errors, error => error.Id == "CS0234" && error.GetMessage().Contains("Abs", StringComparison.Ordinal));
    }

    [Fact]
    public async Task should_reject_a_sibling_shadowing_string_comparison()
    {
        var source = Source.Replace("      readmodel Total", "      readmodel StringComparison\n        id Uuid\n        amount Decimal\n      query ComparisonById => StringComparison?\n        by id Uuid\n      reducer ComparisonFold => StringComparison\n        on OrderPlaced\n          ```csharp\n          return new StringComparison(context.Event.Id, context.Event.Amount);\n          ```\n      readmodel Total", StringComparison.Ordinal)
            .Replace("Guid.Parse(\"00000000-0000-0000-0000-000000000001\")", "context.Event.Id", StringComparison.Ordinal)
            .Replace("return new Total(context.Event.Id, context.Event.Amount);", "return new Total(context.Event.Id, \"x\".Contains(\"x\", StringComparison.Ordinal) ? context.Event.Amount : 0m);", StringComparison.Ordinal);
        var plan = Plan(await Load(source));
        Assert.Contains(plan.Diagnostics, diagnostic => diagnostic.Code == "STAGE-ESM-022" && diagnostic.Message.Contains("StringComparison", StringComparison.Ordinal));
    }

    [Fact]
    public async Task should_plan_cross_slice_reducer_assertions_at_slice_scope()
    {
        var source = WithIdentifierConcept(Source).Replace("Guid.Parse(\"00000000-0000-0000-0000-000000000001\")", "context.Event.Id", StringComparison.Ordinal)
            .Replace("      event OrderPlaced\n        id OrderId\n        amount Decimal", "      event OrderPlaced\n        id OrderId\n        amount Decimal\n      specification CreatingATotal\n        when PlaceOrder\n          id = \"3fa85f64-5717-4562-b3fc-2c963f66afa6\"\n          amount = 20\n        then OrderPlaced\n          id = \"3fa85f64-5717-4562-b3fc-2c963f66afa6\"\n          amount = 20\n        then readmodel Total\n          id = \"3fa85f64-5717-4562-b3fc-2c963f66afa6\"\n          amount = 20", StringComparison.Ordinal);
        var loaded = await Load(source);
        var slice = loaded.Model.Application.Modules.Single().Features.Single().Slices.Single(item => item.Specifications.Length > 0);
        Assert.True(Plan(loaded, new(ArtifactRenderScopeKind.Slice, slice.Id)).Success);
    }

    [Fact]
    public async Task should_reject_unordered_duplicate_events_with_a_reducer_assertion()
    {
        var source = WithIdentifierConcept(Source).Replace("Guid.Parse(\"00000000-0000-0000-0000-000000000001\")", "context.Event.Id", StringComparison.Ordinal)
            .Replace("          amount = amount\n      event", "          amount = amount\n        produces OrderPlaced\n          for id\n          id = id\n          amount = amount\n      event", StringComparison.Ordinal)
            .Replace("      event OrderPlaced\n        id OrderId\n        amount Decimal", "      event OrderPlaced\n        id OrderId\n        amount Decimal\n      specification CreatingATotal\n        when PlaceOrder\n          id = \"3fa85f64-5717-4562-b3fc-2c963f66afa6\"\n          amount = 20\n        then events in any order\n        then OrderPlaced\n          id = \"3fa85f64-5717-4562-b3fc-2c963f66afa6\"\n          amount = 20\n        then OrderPlaced\n          id = \"3fa85f64-5717-4562-b3fc-2c963f66afa6\"\n          amount = 20\n        then readmodel Total\n          id = \"3fa85f64-5717-4562-b3fc-2c963f66afa6\"\n          amount = 20", StringComparison.Ordinal);
        var plan = Plan(await Load(source));
        Assert.Contains(plan.Diagnostics, diagnostic => diagnostic.Code == "STAGE-ESM-011" && diagnostic.Message.Contains("unordered", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task should_reject_a_reducer_descriptor_with_an_incompatible_key_type()
    {
        var loaded = await Load(Source.Replace("Guid.Parse(\"00000000-0000-0000-0000-000000000001\")", "Guid.Empty", StringComparison.Ordinal));
        var descriptor = Assert.Single(loaded.TypedContextDescriptors);
        var changed = descriptor with
        {
            Members = [.. descriptor.Members.Select(member => member.Name == "Key" ? member with
            {
                Type = member.Type with { RuntimeToken = SemanticContextRuntimeTokens.WholeNumber }
            } : member)]
        };
        var plan = Plan(loaded with { TypedContextDescriptors = [changed] });
        Assert.Contains(plan.Diagnostics, diagnostic => diagnostic.Code == "STAGE-ESM-021" && diagnostic.Message.Contains("Key", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("State")]
    [InlineData("Event")]
    [InlineData("Key")]
    [InlineData("Tenant")]
    [InlineData("Occurred")]
    [InlineData("SequenceNumber")]
    [InlineData("IsFirst")]
    public async Task should_reject_a_reducer_descriptor_with_changed_member_nullability(string name)
    {
        var loaded = await Load(Source.Replace("Guid.Parse(\"00000000-0000-0000-0000-000000000001\")", "Guid.Empty", StringComparison.Ordinal));
        var descriptor = Assert.Single(loaded.TypedContextDescriptors);
        var changed = descriptor with
        {
            Members = [.. descriptor.Members.Select(member => member.Name == name ? member with
            {
                IsNullable = !member.IsNullable
            } : member)]
        };
        var plan = Plan(loaded with { TypedContextDescriptors = [changed] });
        Assert.Contains(plan.Diagnostics, diagnostic => diagnostic.Code == "STAGE-ESM-021" && diagnostic.Message.Contains(name, StringComparison.Ordinal));
    }

    [Fact]
    public async Task should_reject_a_reducer_descriptor_with_a_foreign_key_source()
    {
        var loaded = await Load(Source.Replace("Guid.Parse(\"00000000-0000-0000-0000-000000000001\")", "Guid.Empty", StringComparison.Ordinal));
        var descriptor = loaded.TypedContextDescriptors.Single();
        var changed = descriptor with
        {
            Members = [.. descriptor.Members.Select(member => member.Name == "Key" ? member with
            {
                Source = member.Source with { Kind = SemanticContextSourceKinds.ReadModel }
            } : member)]
        };
        var plan = Plan(loaded with { TypedContextDescriptors = [changed] });
        Assert.Contains(plan.Diagnostics, diagnostic => diagnostic.Code == "STAGE-ESM-021" && diagnostic.Message.Contains("Key", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("State")]
    [InlineData("Event")]
    [InlineData("Key")]
    [InlineData("Tenant")]
    [InlineData("Occurred")]
    [InlineData("SequenceNumber")]
    [InlineData("IsFirst")]
    public async Task should_reject_changed_source_identity_for_every_reducer_member(string name)
    {
        var loaded = await Load(Source.Replace("Guid.Parse(\"00000000-0000-0000-0000-000000000001\")", "context.Event.Id", StringComparison.Ordinal));
        var descriptor = Assert.Single(loaded.TypedContextDescriptors);
        var changed = descriptor with
        {
            Members = [.. descriptor.Members.Select(member => member.Name == name ? member with
            {
                Source = member.Source with { SemanticId = member.Source.SemanticId is null ? descriptor.OperationId : null }
            } : member)]
        };
        Assert.Contains(Plan(loaded with { TypedContextDescriptors = [changed] }).Diagnostics,
            diagnostic => diagnostic.Code == "STAGE-ESM-021" && diagnostic.Message.Contains(name, StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("State")]
    [InlineData("Event")]
    [InlineData("Key")]
    [InlineData("Tenant")]
    [InlineData("Occurred")]
    [InlineData("SequenceNumber")]
    [InlineData("IsFirst")]
    public async Task should_reject_changed_revision_and_constant_for_every_reducer_member(string name)
    {
        var loaded = await Load(Source.Replace("Guid.Parse(\"00000000-0000-0000-0000-000000000001\")", "context.Event.Id", StringComparison.Ordinal));
        var descriptor = Assert.Single(loaded.TypedContextDescriptors);
        foreach (var changedSource in new[]
        {
            descriptor.Members.Single(member => member.Name == name).Source with { EventRevision = new(2) },
            descriptor.Members.Single(member => member.Name == name).Source with { ConstantValue = "planted" }
        })
        {
            var changed = descriptor with
            {
                Members = [.. descriptor.Members.Select(member => member.Name == name ? member with { Source = changedSource } : member)]
            };
            Assert.Contains(Plan(loaded with { TypedContextDescriptors = [changed] }).Diagnostics,
                diagnostic => diagnostic.Code == "STAGE-ESM-021" && diagnostic.Message.Contains(name, StringComparison.Ordinal));
        }
    }

    [Fact]
    public async Task should_return_diagnostics_for_specifications_without_reference_assemblies()
    {
        var source = WithIdentifierConcept(Source).Replace("Guid.Parse(\"00000000-0000-0000-0000-000000000001\")", "context.Event.Id", StringComparison.Ordinal)
            .Replace("      event OrderPlaced\n        id OrderId\n        amount Decimal", "      event OrderPlaced\n        id OrderId\n        amount Decimal\n      specification CreatingATotal\n        when PlaceOrder\n          id = \"3fa85f64-5717-4562-b3fc-2c963f66afa6\"\n          amount = 20\n        then OrderPlaced\n          id = \"3fa85f64-5717-4562-b3fc-2c963f66afa6\"\n          amount = 20\n        then readmodel Total\n          id = \"3fa85f64-5717-4562-b3fc-2c963f66afa6\"\n          amount = 20", StringComparison.Ordinal);
        var loaded = await Load(source);
        PureTransitionAdmission.ReferenceDirectoryOverride.Value = Path.Combine(Path.GetTempPath(), $"absent-stage-ref-{Guid.NewGuid():N}");
        try
        {
            var plan = Plan(loaded);
            Assert.Contains(plan.Diagnostics, diagnostic => diagnostic.Code == "STAGE-ESM-021" && diagnostic.Message.Contains("10.0.12", StringComparison.Ordinal));
            Assert.Contains(plan.Diagnostics, diagnostic => diagnostic.Code == "STAGE-ESM-011");
        }
        finally
        {
            PureTransitionAdmission.ReferenceDirectoryOverride.Value = null;
        }
    }

    [Fact]
    public async Task should_reject_a_body_that_only_binds_with_an_extra_common_using()
    {
        var source = WithIdentifierConcept(Source).Replace("Guid.Parse(\"00000000-0000-0000-0000-000000000001\")", "new OrderId(Guid.Empty)", StringComparison.Ordinal);
        var plan = Plan(await Load(source));
        Assert.Contains(plan.Diagnostics, diagnostic => diagnostic.Code == "STAGE-ESM-019" && diagnostic.Message.Contains("OrderId", StringComparison.Ordinal));
    }

    [Fact]
    public async Task should_emit_the_verified_body_byte_for_byte()
    {
        var source = Source.Replace(
            "return new Total(Guid.Parse(\"00000000-0000-0000-0000-000000000001\"), context.Event.Amount);",
            "var marker = @\"first\n          second\"; return new Total(Guid.Empty, marker.Length);",
            StringComparison.Ordinal);
        var loaded = await Load(source);
        var plan = Plan(loaded);
        Assert.True(plan.Success, string.Join(Environment.NewLine, plan.Diagnostics));
        var body = loaded.ImplementationContents.Values.Single();
        var file = plan.Artifacts.Single(_ => _.RelativePath.EndsWith("Fold.cs", StringComparison.Ordinal));
        Assert.Contains(body, System.Text.Encoding.UTF8.GetString(file.Bytes.AsSpan()), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("List<decimal>")]
    [InlineData("decimal[]")]
    public async Task should_reject_mutable_type_tests_on_real_reducer_input(string mutableType)
    {
        var source = Source.Replace("        amount Decimal\n        produces", "        amount Decimal\n        amounts Decimal[]\n        produces", StringComparison.Ordinal)
            .Replace("          amount = amount\n      event", "          amount = amount\n          amounts = amounts\n      event", StringComparison.Ordinal)
            .Replace("      event OrderPlaced\n        id Uuid\n        amount Decimal", "      event OrderPlaced\n        id Uuid\n        amount Decimal\n        amounts Decimal[]", StringComparison.Ordinal);
        var admitted = source.Replace(
            "return new Total(Guid.Parse(\"00000000-0000-0000-0000-000000000001\"), context.Event.Amount);",
            "var sum = 0m; foreach (var amount in context.Event.Amounts) sum += amount; return new Total(context.Event.Id, sum);",
            StringComparison.Ordinal);
        var loaded = await Load(admitted);
        Assert.Contains("amounts Decimal[]", source, StringComparison.Ordinal);
        Assert.Contains("Amounts", string.Join(',', loaded.Model.Application.Modules.SelectMany(module => module.Features).SelectMany(feature => feature.Slices).SelectMany(slice => slice.Events).SelectMany(@event => @event.Properties).Select(property => property.Name)), StringComparison.OrdinalIgnoreCase);
        var plan = Plan(loaded);
        Assert.True(plan.Success, string.Join(Environment.NewLine, plan.Diagnostics));
        var @event = plan.Artifacts.Single(artifact => artifact.RelativePath.EndsWith("PlaceOrder.cs", StringComparison.Ordinal));
        Assert.Contains("ImmutableArray<decimal> Amounts", System.Text.Encoding.UTF8.GetString(@event.Bytes.AsSpan()), StringComparison.Ordinal);
        var files = plan.Artifacts.Where(artifact => artifact.RelativePath.EndsWith(".cs", StringComparison.Ordinal) && artifact.RelativePath != "Program.cs")
            .Select(artifact => new RenderedFile(artifact.RelativePath, System.Text.Encoding.UTF8.GetString(artifact.Bytes.AsSpan())));
        var compilation = RenderedOutput.CreateCompilation(files);
        Assert.Empty(RenderedOutput.Errors(files));
        var request = new ArtifactRenderRequest(
            loaded.Model,
            loaded.Plan,
            CratisRendering.CreateProfile("Projects", new("Projects", "Projects")),
            new(ArtifactRenderScopeKind.Application, loaded.Model.Application.Id))
        {
            TypedContextDescriptors = loaded.TypedContextDescriptors
        };
        var context = new SemanticApplicationContext(request, new("Projects", "Projects"));
        var reducer = Assert.Single(context.Reducers);
        var transition = Assert.Single(reducer.Transitions);
        var descriptor = Assert.Single(loaded.TypedContextDescriptors);
        var synthetic = PureTransitionAdmission.Analyze(
            loaded.ImplementationContents[transition.RequirementId],
            context,
            context.ReadModels[reducer.ReadModel],
            context.Events[transition.EventContract],
            descriptor,
            Assert.Single(loaded.ImplementationRequirements));
        var reducerPath = plan.Artifacts.Single(artifact => artifact.RelativePath.EndsWith("Fold.cs", StringComparison.Ordinal)).RelativePath;
        var real = PureTransitionAdmission.AnalyzeRendered(compilation, reducerPath, descriptor);
        Assert.True(synthetic.Accepted && real.Accepted, $"{synthetic.Reason}; {real.Reason}");
        Assert.Equal(synthetic.BoundSymbols.ToArray(), real.BoundSymbols.ToArray());

        var planted = source.Replace(
            "return new Total(Guid.Parse(\"00000000-0000-0000-0000-000000000001\"), context.Event.Amount);",
            $"if (context.Event.Amounts is {mutableType} values) {{ values[0] += 1m; }} return context.State;",
            StringComparison.Ordinal);
        var refused = Plan(await Load(planted));
        Assert.Contains(refused.Diagnostics, diagnostic => diagnostic.Code == "STAGE-ESM-022" && diagnostic.Message.Contains(mutableType, StringComparison.Ordinal));
    }

    [Fact]
    public async Task should_render_reducer_state_collections_as_immutable_values()
    {
        var source = Source.Replace(
            "      readmodel Total\n        id Uuid\n        amount Decimal",
            "      readmodel Total\n        id Uuid\n        amount Decimal\n        amounts Decimal[]",
            StringComparison.Ordinal)
            .Replace(
                "return new Total(Guid.Parse(\"00000000-0000-0000-0000-000000000001\"), context.Event.Amount);",
                "return context.State is not null && context.State.Amounts.Length >= 0 ? context.State : null;",
                StringComparison.Ordinal);
        var plan = Plan(await Load(source));
        Assert.True(plan.Success, string.Join(Environment.NewLine, plan.Diagnostics));
        var model = plan.Artifacts.Single(artifact => artifact.RelativePath.EndsWith("Totals.cs", StringComparison.Ordinal));
        Assert.Contains("ImmutableArray<decimal> Amounts", System.Text.Encoding.UTF8.GetString(model.Bytes.AsSpan()), StringComparison.Ordinal);
        var files = plan.Artifacts.Where(artifact => artifact.RelativePath.EndsWith(".cs", StringComparison.Ordinal) && artifact.RelativePath != "Program.cs")
            .Select(artifact => new RenderedFile(artifact.RelativePath, System.Text.Encoding.UTF8.GetString(artifact.Bytes.AsSpan())));
        Assert.Empty(RenderedOutput.Errors(files));
    }

    [Fact]
    public async Task should_reject_physical_crlf_inside_verbatim_literals_before_normalization()
    {
        const string body = "return new Total(Guid.Empty, @\"a\r\nb\".Length);";
        var verdict = await Analyze(body);
        Assert.Equal("STAGE-ESM-022", verdict.Code);
        Assert.Contains("Carriage return", verdict.Reason, StringComparison.Ordinal);
        Assert.True((await Analyze(body.Replace("\r\n", "\n", StringComparison.Ordinal))).Accepted);
    }

    [Fact]
    public async Task should_render_the_reducer_and_its_wrapped_body()
    {
        var compilation = await Load(Source.Replace("Guid.Parse(\"00000000-0000-0000-0000-000000000001\")", "Guid.Empty", StringComparison.Ordinal));
        var plan = Plan(compilation);
        Assert.True(plan.Success, $"body={string.Join('|', compilation.ImplementationContents.Values)}\n{string.Join(Environment.NewLine, plan.Diagnostics)}");
        Assert.Contains(plan.Artifacts, artifact => artifact.RelativePath.EndsWith("Fold.cs", StringComparison.Ordinal));
        Assert.Contains(plan.Artifacts, artifact => artifact.RelativePath.StartsWith("TypedContexts/TypedContext_", StringComparison.Ordinal));
        var files = plan.Artifacts.Where(artifact => artifact.RelativePath.EndsWith(".cs", StringComparison.Ordinal) && artifact.RelativePath != "Program.cs")
            .Select(artifact => new RenderedFile(artifact.RelativePath, System.Text.Encoding.UTF8.GetString(artifact.Bytes.AsSpan())));
        var errors = RenderedOutput.Errors(files);
        Assert.True(errors.Count == 0, string.Join(Environment.NewLine, errors));
    }

    [Fact]
    public async Task should_bind_same_typed_constructor_arguments_in_emitted_model_order()
    {
        const string original = "return new Total(Guid.Parse(\"00000000-0000-0000-0000-000000000001\"), context.Event.Amount);";
        var source = WithIdentifierConcept(Source).Replace(
            "      readmodel Total\n        id OrderId\n        amount Decimal",
            "      readmodel Total\n        id OrderId\n        amount Decimal\n        alpha Decimal\n        beta Decimal",
            StringComparison.Ordinal)
            .Replace("      event OrderPlaced\n        id OrderId\n        amount Decimal",
                "      event OrderPlaced\n        id OrderId\n        amount Decimal\n      specification VerifyingTheConstructorOrder\n        when PlaceOrder\n          id = \"3fa85f64-5717-4562-b3fc-2c963f66afa6\"\n          amount = 20\n        then OrderPlaced\n          id = \"3fa85f64-5717-4562-b3fc-2c963f66afa6\"\n          amount = 20\n        then readmodel Total\n          id = \"3fa85f64-5717-4562-b3fc-2c963f66afa6\"\n          amount = 11\n          alpha = 22\n          beta = 33",
                StringComparison.Ordinal);
        var baseline = await Load(source.Replace(original, "return context.State;", StringComparison.Ordinal));
        var model = baseline.Model.Application.Modules.Single().Features.Single().Slices.Single(slice => slice.ReadModels.Length > 0).ReadModels.Single();
        var ordered = SemanticStateViewArtifactRenderer.OrderedProperties(model.Properties).ToArray();
        Assert.NotEqual(model.Properties.Where(property => property.Type.Primitive == SemanticPrimitiveType.DecimalNumber).Select(property => property.Name),
            ordered.Where(property => property.Type.Primitive == SemanticPrimitiveType.DecimalNumber).Select(property => property.Name));
        var values = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["id"] = "context.Event.Id",
            ["amount"] = "11m",
            ["alpha"] = "22m",
            ["beta"] = "33m"
        };
        var body = $"return new Total({string.Join(", ", ordered.Select(property => values[property.Name]))});";
        var loaded = await Load(source.Replace(original, body, StringComparison.Ordinal));
        var plan = Plan(loaded);
        Assert.True(plan.Success, string.Join(Environment.NewLine, plan.Diagnostics));
        var files = plan.Artifacts.Where(artifact => artifact.RelativePath.EndsWith(".cs", StringComparison.Ordinal) && artifact.RelativePath != "Program.cs")
            .Select(artifact => new RenderedFile(artifact.RelativePath, System.Text.Encoding.UTF8.GetString(artifact.Bytes.AsSpan()))).ToArray();
        Assert.Empty(RenderedOutput.Errors(files));
        var specification = plan.Artifacts.Single(artifact => artifact.RelativePath.Contains("verifying_the_constructor_order_is_projected", StringComparison.Ordinal));
        var specificationText = System.Text.Encoding.UTF8.GetString(specification.Bytes.AsSpan());
        Assert.Contains(".Amount.ShouldEqual(11m)", specificationText, StringComparison.Ordinal);
        Assert.Contains(".Alpha.ShouldEqual(22m)", specificationText, StringComparison.Ordinal);
        Assert.Contains(".Beta.ShouldEqual(33m)", specificationText, StringComparison.Ordinal);
        var assembly = RenderedOutput.Load(files);
        var record = assembly.GetTypes().Single(type => type.Name == "Total");
        Assert.Equal(ordered.Select(property => Identifiers.ToPascalCase(property.Name)),
            record.GetConstructors().Single(constructor => constructor.GetParameters().Length == ordered.Length).GetParameters().Select(parameter => parameter.Name));
        var wrapper = assembly.GetTypes().Single(type => type.Name.StartsWith("TypedContext_", StringComparison.Ordinal));
        var @event = assembly.GetTypes().Single(type => type.Name == "OrderPlaced");
        var tenant = assembly.GetType("Projects.TypedContexts.TenantId");
        var identity = assembly.GetTypes().Single(type => type.Name == "OrderId");
        var payload = Activator.CreateInstance(@event, Activator.CreateInstance(identity, Guid.NewGuid()), 1m);
        var context = Activator.CreateInstance(wrapper, null, payload, "key", Activator.CreateInstance(tenant, "Default"), DateTimeOffset.UtcNow, 0L);
        var reducer = assembly.GetTypes().Single(type => type.Name == "Fold");
        var transition = reducer.GetMethods(BindingFlags.Static | BindingFlags.NonPublic).Single(method => method.Name.StartsWith("Transition_", StringComparison.Ordinal));
        var result = transition.Invoke(null, [context]);
        Assert.Equal(11m, record.GetProperty("Amount").GetValue(result));
        Assert.Equal(22m, record.GetProperty("Alpha").GetValue(result));
        Assert.Equal(33m, record.GetProperty("Beta").GetValue(result));
    }

    [Fact]
    public async Task should_generate_a_keyed_reducer_read_model_specification()
    {
        var source = WithIdentifierConcept(Source).Replace("Guid.Parse(\"00000000-0000-0000-0000-000000000001\")", "context.Event.Id", StringComparison.Ordinal)
            .Replace("      event OrderPlaced\n        id OrderId\n        amount Decimal", "      event OrderPlaced\n        id OrderId\n        amount Decimal\n      specification CreatingATotal\n        when PlaceOrder\n          id = \"3fa85f64-5717-4562-b3fc-2c963f66afa6\"\n          amount = 20\n        then OrderPlaced\n          id = \"3fa85f64-5717-4562-b3fc-2c963f66afa6\"\n          amount = 20\n        then readmodel Total\n          id = \"3fa85f64-5717-4562-b3fc-2c963f66afa6\"\n          amount = 20", StringComparison.Ordinal);
        Assert.Contains("specification CreatingATotal", source, StringComparison.Ordinal);
        var plan = Plan(await Load(source));
        Assert.True(plan.Success, string.Join(Environment.NewLine, plan.Diagnostics));
        var generatedSpec = plan.Artifacts.Single(artifact => artifact.RelativePath.Contains("is_projected", StringComparison.Ordinal));
        Assert.Contains("[Fact]", System.Text.Encoding.UTF8.GetString(generatedSpec.Bytes.AsSpan()), StringComparison.Ordinal);
        var files = plan.Artifacts.Where(artifact => artifact.RelativePath.EndsWith(".cs", StringComparison.Ordinal) && artifact.RelativePath != "Program.cs")
            .Select(artifact => new RenderedFile(artifact.RelativePath, System.Text.Encoding.UTF8.GetString(artifact.Bytes.AsSpan())));
        var errors = RenderedOutput.Errors(files);
        Assert.True(errors.Count == 0, string.Join(Environment.NewLine, errors));
    }

    [Fact]
    public async Task should_render_file_and_inline_bodies_identically()
    {
        var inline = Source.Replace("Guid.Parse(\"00000000-0000-0000-0000-000000000001\")", "Guid.Empty", StringComparison.Ordinal);
        var file = inline.Replace("```csharp\n          return new Total(Guid.Empty, context.Event.Amount);\n          ```", "file Reducers/Fold.cs", StringComparison.Ordinal);
        Assert.NotEqual(inline, file);
        var first = Plan(await Load(inline));
        var second = Plan(await Load(file));
        Assert.True(first.Success, string.Join(Environment.NewLine, first.Diagnostics));
        Assert.True(second.Success, string.Join(Environment.NewLine, second.Diagnostics));
        var firstContent = System.Text.Encoding.UTF8.GetString(first.Artifacts.Single(_ => _.RelativePath.EndsWith("Fold.cs", StringComparison.Ordinal)).Bytes.AsSpan());
        var secondContent = System.Text.Encoding.UTF8.GetString(second.Artifacts.Single(_ => _.RelativePath.EndsWith("Fold.cs", StringComparison.Ordinal)).Bytes.AsSpan());
        Assert.True(firstContent == secondContent, $"Inline:\n{firstContent}\nFile:\n{secondContent}");
    }

    [Fact]
    public async Task should_keep_the_generated_tenant_token_in_reflection_parity_with_screenplay()
    {
        var compiled = await Load(Source.Replace("Guid.Parse(\"00000000-0000-0000-0000-000000000001\")", "Guid.Empty", StringComparison.Ordinal));
        var plan = Plan(compiled);
        Assert.True(plan.Success, string.Join(Environment.NewLine, plan.Diagnostics));
        var token = plan.Artifacts.Single(_ => _.RelativePath == "TypedContexts/TenantId.cs");
        var assembly = RenderedOutput.Load([new RenderedFile(token.RelativePath, System.Text.Encoding.UTF8.GetString(token.Bytes.AsSpan()))]);
        var actual = assembly.GetType("Projects.TypedContexts.TenantId");
        var expected = typeof(Screenplay.Contexts.TenantId);
        Assert.Equal(Members(expected), Members(actual));
        Assert.Equal(Screenplay.Contexts.TenantId.Default.Value, actual.GetField("Default").GetValue(null).GetType().GetProperty("Value").GetValue(actual.GetField("Default").GetValue(null)));
        Assert.Equal(Screenplay.Contexts.TenantId.NotSet.Value, actual.GetField("NotSet").GetValue(null).GetType().GetProperty("Value").GetValue(actual.GetField("NotSet").GetValue(null)));

        static string[] Members(Type type) => [.. type.GetMembers(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
            .Select(member => member switch
            {
                MethodInfo method => $"method/{member.Name}/{Normalize(method.ReturnType)}/{string.Join(',', method.GetParameters().Select(parameter => Normalize(parameter.ParameterType)))}",
                ConstructorInfo constructor => $"constructor/{member.Name}/{string.Join(',', constructor.GetParameters().Select(parameter => Normalize(parameter.ParameterType)))}",
                FieldInfo field => $"field/{member.Name}/{Normalize(field.FieldType)}",
                PropertyInfo property => $"property/{member.Name}/{Normalize(property.PropertyType)}",
                _ => $"{member.MemberType}/{member.Name}"
            }).Order(StringComparer.Ordinal)];

        static string Normalize(Type type) => (type.FullName ?? type.Name)
            .Replace("Projects.TypedContexts", "Cratis.Screenplay.Contexts", StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("return new Total(Guid.NewGuid(), context.Event.Amount);", "Guid.NewGuid")]
    [InlineData("return new Total(Guid.Empty, DateTime.UtcNow.Day);", "DateTime.UtcNow")]
    [InlineData("return new Total(Guid.Empty, Environment.TickCount);", "Environment.TickCount")]
    [InlineData("Environment.ExitCode = 1; return context.State;", "static write")]
    [InlineData("var alias = context; return new Total(Guid.Empty, alias.Tenant.Value.Length);", "context.Tenant alias")]
    [InlineData("return new Total(Guid.Empty, System.IO.File.ReadAllText(\"x\").Length);", "System.IO.File")]
    [InlineData("return new Total(Guid.Empty, new System.Net.Http.HttpClient().Timeout.Seconds);", "HttpClient")]
    [InlineData("return new Total(Guid.Empty, new System.Threading.Thread(() => { }).ManagedThreadId);", "Thread")]
    [InlineData("return new Total(Guid.Empty, System.Threading.Tasks.Task.CompletedTask.Id);", "Task")]
    [InlineData("var number = 0; return new Total(Guid.Empty, System.Threading.Interlocked.Increment(ref number));", "Interlocked")]
    [InlineData("return new Total(Guid.Empty, typeof(Total).GetProperties().Length);", "reflection")]
    [InlineData("return new Total(Guid.Empty, new Random().Next());", "Random")]
    [InlineData("return new Total(Guid.Empty, typeof(Total).GetMethod(\"ToString\")!.MetadataToken);", "reflection metadata")]
    [InlineData("dynamic value = context.Event.Amount; return new Total(Guid.Empty, value);", "dynamic")]
    [InlineData("lock (context) { return context.State; }", "lock")]
    [InlineData("#if DEBUG\nreturn context.State;\n#endif", "directive")]
    [InlineData("return new Total(Guid.Empty, 1.5m.ToString().Length);", "culture formatting")]
    [InlineData("[System.Runtime.InteropServices.DllImport(\"libc\", EntryPoint=\"system\")] static extern int Equals(string c); Equals(\"true\"); return context.State;", "Equals extern native local")]
    [InlineData("[System.Obsolete] int Equals() => 1; Equals(); return context.State;", "local function attribute")]
    [InlineData("int Equals([System.Runtime.InteropServices.Optional] int value = 0) => value; Equals(); return context.State;", "local parameter attribute")]
    [InlineData("Func<int, int> f = [System.Obsolete] (int x) => x; return new Total(Guid.Empty, f(1));", "lambda attribute")]
    [InlineData("Func<int, int> f = ([System.Runtime.InteropServices.Optional] int x) => x; return new Total(Guid.Empty, f(1));", "lambda parameter attribute")]
    [InlineData("_ = new Microsoft.Win32.SafeHandles.SafeFileHandle((nint)3, true); return context.State;", "SafeFileHandle constructor")]
    [InlineData("return new Total(Guid.Empty, new[] { 1, 2, 3 }.Shuffle().First());", "Enumerable.Shuffle")]
    [InlineData("return new Total(Guid.Empty, context.Event.GetHashCode() % 2);", "record.GetHashCode")]
    [InlineData("return new Total(Guid.Empty, EqualityComparer<string>.Default.GetHashCode(\"x\"));", "EqualityComparer.GetHashCode")]
    [InlineData("return new Total(Guid.Empty, System.Collections.Immutable.ImmutableHashSet.Create(\"a\", \"b\").First().Length);", "ImmutableHashSet enumeration")]
    [InlineData("return new Total(Guid.Empty, new Dictionary<string, int> { [\"x\"] = 1 }.First().Value);", "Dictionary enumeration")]
    [InlineData("return new Total(Guid.Empty, new Dictionary<string, int>().Count);", "Dictionary construction")]
    [InlineData("var (s, e, k, t, o, n) = context; return new Total(Guid.Empty, t.Value.Length);", "wrapper deconstruction")]
    [InlineData("return context is (_, _, _, var t, _, _) ? new Total(Guid.Empty, t.Value.Length) : null;", "wrapper positional pattern")]
    [InlineData("context.Deconstruct(out var s, out var e, out var k, out var t, out var o, out var n); return new Total(Guid.Empty, t.Value.Length);", "wrapper Deconstruct call")]
    [InlineData("return new Total(Guid.Empty, context.Equals(context with { Tenant = new(\"Default\") }) ? 1 : 0);", "wrapper Equals/with")]
    [InlineData("return new Total(Guid.Empty, (context is { Tenant: { Value: var t } }) ? t.Length : 0);", "wrapper recursive pattern")]
    [InlineData("return new Total(Guid.Empty, (context with { Occurred = DateTimeOffset.MinValue }).Occurred.Day);", "wrapper with/Occurred")]
    [InlineData("return new Total(Guid.Empty, (context with { SequenceNumber = 1 }).SequenceNumber);", "wrapper with/SequenceNumber")]
    [InlineData("return new Total(Guid.Empty, context.Equals(context) ? 1 : 0);", "passing context as value")]
    [InlineData("return new Total(Guid.Empty, (\"x\" + 1.5m).Length);", "culture concatenation")]
    [InlineData("return new Total(Guid.Empty, string.Concat((object)1.5m).Length);", "object Concat")]
    [InlineData("return new Total(Guid.Empty, \"a\".Contains(\"A\", StringComparison.CurrentCultureIgnoreCase) ? 1 : 0);", "CurrentCulture comparison")]
    [InlineData("return new Total(Guid.Empty, 1.5m.ToString((IFormatProvider?)null).Length);", "null culture provider")]
    [InlineData("return new Total(Guid.Empty, decimal.Parse(\"1,5\", (IFormatProvider?)null));", "null parse provider")]
    [InlineData("return new Total(Guid.Empty, char.ToUpper('a'));", "char.ToUpper")]
    [InlineData("return new Total(Guid.Empty, DateTime.MinValue.ToShortDateString().Length);", "ToShortDateString")]
    [InlineData("return new Total(Guid.Empty, DateTimeOffset.MinValue.ToLocalTime().Day);", "ToLocalTime")]
    [InlineData("return new Total(Guid.Empty, DateTimeOffset.MinValue.LocalDateTime.Hour);", "LocalDateTime")]
    [InlineData("return new Total(Guid.Empty, new[] { \"a\", \"b\" }.OrderBy(s => s, Comparer<string>.Default).First().Length);", "culture default comparer")]
    [InlineData("return new Total(Guid.Empty, new[] { 1, 2 }[0..1].Length);", "range operation")]
    [InlineData("return new Total(Guid.Empty, System.Collections.Immutable.ImmutableDictionary.Create<string, int>().Count);", "ImmutableDictionary hash")]
    [InlineData("_ = new { Value = \"x\" }; return context.State;", "anonymous type")]
    [InlineData("return new $WRAPPER$(context.State, context.Event, context.Key, new Projects.TypedContexts.TenantId(\"Default\"), context.Occurred, context.SequenceNumber).State;", "wrapper construction")]
    [InlineData("return new Total(Guid.Empty, context.Equals(new $WRAPPER$(context.State, context.Event, context.Key, new Projects.TypedContexts.TenantId(\"Default\"), context.Occurred, context.SequenceNumber)) ? 1 : 0);", "wrapper Equals construction")]
    [InlineData("return new Total(Guid.Empty, new HashSet<string> { \"x\" }.Count);", "HashSet hash")]
    [InlineData("return new Total(Guid.Empty, new SortedSet<string> { \"a\" }.Count);", "SortedSet culture")]
    [InlineData("return new Total(Guid.Empty, 1.5m.ToString(System.Globalization.CultureInfo.CurrentCulture).Length);", "explicit current culture provider")]
    [InlineData("return new Total(Guid.Empty, string.Equals(\"i\", \"I\", (StringComparison)1) ? 1 : 0);", "comparison cast bypass")]
    [InlineData("return new Total(Guid.Empty, \"i\".Replace(\"I\", \"xx\", true, null).Length);", "Replace ambient culture")]
    [InlineData("string text = \"x\"; text += 1.5m; return new Total(Guid.Empty, text.Length);", "compound culture concatenation")]
    [InlineData("return new Total(Guid.Empty, DateTime.ParseExact(\"12:00\", \"HH:mm\", System.Globalization.CultureInfo.InvariantCulture).Day);", "DateTime.ParseExact")]
    [InlineData("return new Total(Guid.Empty, new DateTime(2020,1,1).ToUniversalTime().Day);", "DateTime.ToUniversalTime")]
    [InlineData("DateTimeOffset value = new DateTime(2020,1,1); return new Total(Guid.Empty, value.Day);", "implicit DateTime offset conversion")]
    [InlineData("return new Total(Guid.Empty, double.ConvertToIntegerNative<int>(double.PositiveInfinity));", "floating native conversion")]
    [InlineData("nint value = int.MaxValue; value = unchecked(value + 1); return new Total(Guid.Empty, value);", "native-sized integer")]
    [InlineData("return new Total(Guid.Empty, string.Equals(\"é\", \"é\", StringComparison.InvariantCulture) ? 1 : 0);", "InvariantCulture collation")]
    [InlineData("IReadOnlyList<decimal> amounts = new decimal[] { 1m }; if (amounts is List<decimal> values) { values[0] += 1m; } return context.State;", "mutable input list test")]
    [InlineData("IReadOnlyList<decimal> amounts = new decimal[] { 1m }; if (amounts is decimal[] values) { values[0] += 1m; } return context.State;", "mutable input array test")]
    [InlineData("throw new System.ComponentModel.Win32Exception();", "Win32Exception native state")]
    [InlineData("while (true) { }", "unbounded while loop")]
    [InlineData("int F() => F(); return new Total(Guid.Empty, F());", "local recursion")]
    [InlineData("_ = Math.Pow(2, 3); return context.State;", "Math.Pow floating")]
    [InlineData("return new Total(Guid.Empty, typeof(TransitionAnalysis).Name.Length);", "analysis helper typeof")]
    [InlineData("_ = default(decimal); return context.State;", "non-generated default")]
    [InlineData("_ = DateTimeOffset.MinValue.UtcDateTime; return context.State;", "UtcDateTime")]
    [InlineData("_ = context.Occurred.Offset; return context.State;", "Offset")]
    [InlineData("_ = new DateTimeOffset(new DateTime(2020,1,1)); return context.State;", "DateTimeOffset(DateTime)")]
    [InlineData("_ = DateTime.FromFileTime(1); return context.State;", "DateTime.FromFileTime")]
    [InlineData("_ = DateTimeOffset.FromFileTime(1).ToFileTime(); return context.State;", "ToFileTime")]
    [InlineData("_ = DateTime.MinValue.IsDaylightSavingTime(); return context.State;", "IsDaylightSavingTime")]
    [InlineData("_ = double.MultiplyAddEstimate(1,2,3); return context.State;", "double.MultiplyAddEstimate")]
    [InlineData("_ = double.MinNative(1,2); return context.State;", "double.MinNative")]
    [InlineData("_ = double.MaxNative(1,2); return context.State;", "double.MaxNative")]
    [InlineData("_ = double.Sin(1); return context.State;", "double.Sin")]
    [InlineData("_ = new System.Numerics.BigInteger(1); return context.State;", "BigInteger")]
    [InlineData("_ = (Half)1; return context.State;", "Half")]
    [InlineData("_ = 1f; return context.State;", "float")]
    [InlineData("_ = 1d; return context.State;", "double")]
    [InlineData("nuint value = 1; return context.State;", "nuint")]
    [InlineData("do { return context.State; } while (true);", "do loop")]
    [InlineData("for (var i = 0; i < 1; i++) { } return context.State;", "for loop")]
    [InlineData("goto done; done: return context.State;", "goto")]
    [InlineData("try { return context.State; } catch { return null; }", "try catch")]
    [InlineData("using var value = new System.IO.MemoryStream(); return context.State;", "using")]
    [InlineData("return new Total(Guid.Empty, sizeof(int));", "sizeof")]
    [InlineData("return new Total(Guid.Empty, nameof(Total).Length);", "nameof")]
    [InlineData("_ = \"ab\".Trim('a'); return context.State;", "Trim with arguments")]
    [InlineData("var n = 0; _ = new[] { 1 }.Select(x => ++n).Sum(); return context.State;", "mutated captured local")]
    [InlineData("_ = new[] { 2, 1 }.Distinct().Count(); return context.State;", "LINQ Distinct")]
    [InlineData("_ = new[] { 2, 1 }.OrderBy(x => x).First(); return context.State;", "LINQ OrderBy")]
    [InlineData("_ = new[] { 2, 1 }.GroupBy(x => x).First(); return context.State;", "LINQ GroupBy")]
    [InlineData("_ = new[] { 2, 1 }.ToHashSet().Count; return context.State;", "LINQ ToHashSet")]
    [InlineData("_ = (StringComparison)5; return context.State;", "comparison cast")]
    [InlineData("_ = new InvalidOperationException(\"unused\"); return context.State;", "exception construction without throw")]
    [InlineData("_ = (IEnumerable<decimal>)new[] { 1m }; return context.State;", "interface cast")]
    [InlineData("_ = (byte)1; return context.State;", "narrowing integer cast")]
    [InlineData("_ = StringComparison.Ordinal; return context.State;", "standalone StringComparison")]
    [InlineData("_ = MidpointRounding.ToEven; return context.State;", "standalone MidpointRounding")]
    [InlineData("_ = System.Globalization.CultureInfo.InvariantCulture; return context.State;", "standalone CultureInfo")]
    [InlineData("var value = System.Collections.Immutable.ImmutableArray<int>.Empty; System.Collections.Immutable.ImmutableInterlocked.InterlockedInitialize(ref value, System.Collections.Immutable.ImmutableArray<int>.Empty); return context.State;", "ImmutableInterlocked")]
    public async Task should_reject_planted_impure_bodies(string body, string category)
    {
        var verdict = await Analyze(body);
        Assert.False(verdict.Accepted, $"Planted {category} violation was admitted.");
        Assert.True(verdict.Code == "STAGE-ESM-022", $"Planted {category} was rejected by {verdict.Code}: {verdict.Reason}, not the pure gate.");
        var namedSymbol = category switch
        {
            "Equals extern native local" or "local function attribute" => "Equals",
            "SafeFileHandle constructor" => "SafeFileHandle",
            "Enumerable.Shuffle" => null, // Outer composition now refuses before reaching Shuffle.
            "record.GetHashCode" or "EqualityComparer.GetHashCode" => "GetHashCode",
            "ImmutableHashSet enumeration" => null,
            "ImmutableDictionary hash" => "ImmutableDictionary",
            "Dictionary enumeration" => "KeyValuePair",
            "Dictionary construction" => "Dictionary",
            "HashSet hash" => "HashSet",
            "SortedSet culture" => "SortedSet",
            "wrapper Deconstruct call" or
            "wrapper Equals/with" or "wrapper with/Occurred" or
            "wrapper with/SequenceNumber" or "wrapper construction" or "wrapper Equals construction" or
            "passing context as value" => "TypedContext_",
            "culture concatenation" => null,
            "object Concat" => "Concat",
            "CurrentCulture comparison" => null,
            "null culture provider" => null,
            "null parse provider" => "decimal.Parse",
            "char.ToUpper" => null,
            "ToShortDateString" => null,
            "ToLocalTime" => "ToLocalTime",
            "LocalDateTime" => null,
            "ImmutableInterlocked" => "ImmutableInterlocked",
            _ => null
        };
        if (namedSymbol is not null)
        {
            Assert.Contains(namedSymbol, verdict.Reason, StringComparison.Ordinal);
        }
    }

    // Planted adversarial bodies: each was admitted by the pre-fix gate (see failing targeted test).
    [Theory]
    [InlineData("return new Total(Guid.Empty, new[] { 2147483647, -2147483647, 0, 0, 2147483647, -2147483647, 0, 0, 2147483647, -2147483647, 0, 0, 2147483647, -2147483647, 0, 0 }.Sum());")]
    [InlineData("return new Total(Guid.Empty, new[] { 0 }.Select(n => { new[] { 1 }.Select(ignored => ++n).Any(); return n; }).First());")]
    [InlineData("return new Total(Guid.Empty, new[] { 0 }.Select(n => 1 / n).Any() ? 1m : 0m);")]
    [InlineData("IEnumerable<int> values = new[] { 1 }; values = values.Select(ignored => values.First()); return new Total(Guid.Empty, values.First());")]
    [InlineData("return new Total(Guid.Empty, string.Equals(\"\\u0264\", \"\\uA7CB\", StringComparison.OrdinalIgnoreCase) ? 1m : 0m);")]
    [InlineData("return new Total(Guid.Empty, \" \\u2000 \".Trim().Length);")]
    [InlineData("return new Total(Guid.Empty, string.IsNullOrWhiteSpace(\"\\u2000\") ? 1m : 0m);")]
    [InlineData("var projected = new[] { 1 }.Select(n => ++n); return context.State;")]
    [InlineData("var projected = new[] { 1 }.Select(n => n / (n + 1)); return context.State;")]
    [InlineData("var projected = new[] { 1 }.Select(n => checked(n + 1)); return context.State;")]
    [InlineData("var total = new[] { context.Event.Amount }.Aggregate(0m, (a,b) => a + b); return new Total(Guid.Empty, total);")]
    [InlineData("IEnumerable<int> values = new[] { 1 }; var projected = new[] { 0 }.Select(n => values.First()); return context.State;")]
    [InlineData("var numbers = System.Collections.Immutable.ImmutableArray<int>.Empty; var projected = new[] { 0 }.Select(n => numbers.Length); return context.State;")]
    [InlineData("return new Total(Guid.Empty, new[] { 1 }.Any() ? 1m : 0m);")]
    [InlineData("return new Total(Guid.Empty, new[] { 1 }.Count());")]
    [InlineData("return new Total(Guid.Empty, new[] { 1 }.Select(n => n + 1).Count(n => n > 0));")]
    [InlineData("IEnumerable<int> values = true ? new[] { 1 }.Select(n => n + 1) : new[] { 2 }; return new Total(Guid.Empty, values.First());")]
    [InlineData("var values = new[] { 1 }.Concat(new[] { 2 }.Select(n => n + 1)); return context.State;")]
    [InlineData("return new Total(Guid.Empty, System.Collections.Immutable.ImmutableArray.ToImmutableArray(new[] { 1 }.Select(n => n + 1)).Length);")]
    public async Task should_refuse_adversarial_pure_subset_bodies(string body)
    {
        var verdict = await Analyze(body);
        Assert.Equal("STAGE-ESM-022", verdict.Code);
    }

    [Theory]
    [InlineData("Math", "Math.Abs(-1)")]
    [InlineData("String", "string.IsNullOrEmpty(\"x\") ? 1m : 0m")]
    [InlineData("Enumerable", "new[] { 1m }.Sum()")]
    [InlineData("ImmutableArray", "System.Collections.Immutable.ImmutableArray<decimal>.Empty.Length")]
    [InlineData("DateTimeOffset", "context.Occurred.Day")]
    [InlineData("TimeSpan", "(context.Occurred - context.Occurred).Ticks")]
    public async Task should_refuse_reducer_names_shadowing_audited_types(string name, string expression)
    {
        var source = Source.Replace("reducer Fold => Total", $"reducer {name} => Total", StringComparison.Ordinal)
            .Replace("Guid.Parse(\"00000000-0000-0000-0000-000000000001\")", "Guid.Empty", StringComparison.Ordinal)
            .Replace("context.Event.Amount);", $"{expression});", StringComparison.Ordinal);
        var plan = Plan(await Load(source));
        Assert.Contains(plan.Diagnostics, diagnostic => diagnostic.Code == "STAGE-ESM-022" && diagnostic.Message.Contains(name, StringComparison.Ordinal));
    }

    internal static readonly string[] AdmittedFixtures =
    [
            "return new Total(Guid.Empty, context.Event.Amount + 1m);",
            "return new Total(Guid.Empty, Math.Abs(-1m));",
            "return new Total(Guid.Empty, context.Occurred.AddTicks(1).Day);",
            "return new Total(Guid.Empty, \"a\".Contains(\"a\", StringComparison.Ordinal) ? 1m : 0m);",
            "return new Total(Guid.Empty, 1.5m.ToString(System.Globalization.CultureInfo.InvariantCulture).Length);",
            "return new Total(Guid.Empty, System.Collections.Immutable.ImmutableArray<decimal>.Empty.Length);",
            "var sum = 0m; foreach (var item in new[] { 1m }) sum += item; return new Total(Guid.Empty, sum);",
            "return new Total(Guid.Empty, new[] { 1m }.Sum());",
            "throw new InvalidOperationException(\"invalid\");",
            "return new Total(Guid.Empty, Math.Min(2m, Math.Max(1m, Math.Clamp(3m, 0m, 4m))));",
            "return new Total(Guid.Empty, Math.Sign(-1m));",
            "return new Total(Guid.Empty, Math.Round(1.5m, MidpointRounding.ToEven) + Math.Truncate(1.5m) + Math.Floor(1.5m) + Math.Ceiling(1.5m));",
            "return new Total(Guid.Empty, context.Occurred.Year + context.Occurred.Month + context.Occurred.Hour + context.Occurred.Minute + context.Occurred.Second + context.Occurred.Ticks);",
            "return new Total(Guid.Empty, (context.Occurred - context.Occurred).Ticks);",
            "return new Total(Guid.Empty, (context.Occurred + (context.Occurred - context.Occurred)).Day);",
            "return new Total(Guid.Empty, string.Equals(\"a\", \"A\", StringComparison.Ordinal) && \"ab\".StartsWith(\"a\", StringComparison.Ordinal) ? 1m : 0m);",
            "return new Total(Guid.Empty, \"ab\".EndsWith(\"B\", StringComparison.Ordinal) && \"ab\".IndexOf(\"a\", StringComparison.Ordinal) == 0 ? 1m : 0m);",
            "return new Total(Guid.Empty, \"ab\".Substring(0,1)[0] == 'a' ? 1m : 0m);",
            "return new Total(Guid.Empty, string.IsNullOrEmpty(\"\") && !string.IsNullOrEmpty(\" \") ? 1m : 0m);",
            "string text = \"a\"; text += 'b'; return new Total(Guid.Empty, text.Length);",
            "return new Total(Guid.Empty, context.State?.Amount ?? 0m);",
            "return context.State is null ? new Total(Guid.Empty, 0m) : context.State;",
            "return context.State is not null ? context.State with { Amount = 1m } : new Total(Guid.Empty, 0m);",
            "if (context.Event.Amount is 1m) return new Total(Guid.Empty, 1m); else return context.State;",
            "switch (context.Event.Amount) { case 1m: return context.State; default: return new Total(Guid.Empty, 0m); }",
            "var selected = new[] { 1m, 2m }.Select(n => n); return new Total(Guid.Empty, new[] { 1m, 2m }.Sum());",
            "var filtered = new[] { 1m, 2m }.Where(n => n > 1m); return new Total(Guid.Empty, new[] { 1m }.Sum());",
            "return new Total(Guid.Empty, new[] { 1m }.Count(n => n > 0m) + (new[] { 1m }.Any(n => n > 0m) && new[] { 1m }.All(n => n > 0m) ? 1m : 0m));",
            "var taken = new[] { 1m, 2m }.Take(1); var skipped = new[] { 3m }.Skip(0); var joined = new[] { 1m }.Concat(new[] { 2m }); return new Total(Guid.Empty, new[] { 1, 2 }.Aggregate(0, (a,b) => a+b));",
            "return new Total(Guid.Empty, new[] { 1m }.First() + new[] { 1m }.FirstOrDefault() + new[] { 1m }.Last() + new[] { 1m }.LastOrDefault());",
            "return new Total(Guid.Empty, new[] { 1m }.ToArray()[0]);",
            "IEnumerable<decimal> values = new[] { 1m }; return new Total(Guid.Empty, System.Collections.Immutable.ImmutableArray.ToImmutableArray(values).Length);",
            "throw new ArgumentException(\"invalid\");",
            "return new Total(Guid.Empty, checked(context.Event.Amount + 1m));",
            "return new Total(Guid.Empty, unchecked(context.Event.Amount + 1m));",
            "return new Total(Guid.Empty, Math.Abs(-1));",
            "return new Total(Guid.Empty, (long)1);",
            "return new Total(Guid.Empty, (int)1m);",
            "return default(Total);",
            "return new Total(Guid.Empty, context.Occurred >= context.Occurred ? 1m : 0m);",
            "return new Total(Guid.Empty, (context.Occurred - context.Occurred).Add(context.Occurred - context.Occurred).Ticks);",
            "return new Total(Guid.Empty, (context.Occurred - context.Occurred) == (context.Occurred - context.Occurred) ? 1m : 0m);",
            "return context.State is Total total ? total : new Total(Guid.Empty, 0m);",
            "return context.Event.Amount switch { 1m => new Total(Guid.Empty, 1m), _ => context.State };",
            "var sum = 0m; foreach (var item in System.Collections.Immutable.ImmutableArray<decimal>.Empty) sum += item; return new Total(Guid.Empty, sum);",
            "IEnumerable<decimal> values = new[] { 1m }; return new Total(Guid.Empty, System.Collections.Immutable.ImmutableArray.ToImmutableArray(values)[0]);",
            "return new Total(Guid.Empty, (\"a\" + 'b').Length);",
            "return new Total(Guid.Empty, context.IsFirst && context.Key.Length > 0 ? context.SequenceNumber : 0m);"
    ];

    [Fact]
    public void should_use_only_the_exact_audited_reference_pack()
    {
        Assert.True(PureTransitionAdmission._references.Value.Length > 150);
        Assert.Contains(PureTransitionAdmission._references.Value, reference =>
            reference.Display == $"PureTransitionReferences.{PureTransitionAdmission.ReferencePackVersion}.System.IO.dll");
        Assert.All(PureTransitionAdmission._references.Value, reference =>
            Assert.StartsWith($"PureTransitionReferences.{PureTransitionAdmission.ReferencePackVersion}.", reference.Display, StringComparison.Ordinal));
    }

    [Fact]
    public async Task should_exercise_every_allowlist_entry_in_admitted_fixtures()
    {
        var used = new HashSet<string>(StringComparer.Ordinal);
        var members = new HashSet<string>(StringComparer.Ordinal);
        foreach (var fixture in AdmittedFixtures)
        {
            var verdict = await Analyze(fixture);
            Assert.True(verdict.Accepted, $"Fixture {fixture} was refused: {verdict.Code}: {verdict.Reason}");
            used.UnionWith(verdict.UsedAllowlistEntries);
            members.UnionWith(verdict.AuditedSymbols);
        }

        Assert.Equal(PureTransitionAdmission.AllowlistReasons.Keys.Order(StringComparer.Ordinal), used.Order(StringComparer.Ordinal));
        Assert.Equal(PureTransitionAdmission.AuditedMemberSignatures.Order(StringComparer.Ordinal), members.Order(StringComparer.Ordinal));
    }

    [Fact]
    public async Task should_match_the_real_rendered_compilation_verdict_for_every_admitted_fixture()
    {
        var compiled = await Load(Source.Replace("Guid.Parse(\"00000000-0000-0000-0000-000000000001\")", "Guid.Empty", StringComparison.Ordinal));
        var plan = Plan(compiled);
        Assert.True(plan.Success, string.Join(Environment.NewLine, plan.Diagnostics));
        Assert.Contains(plan.Diagnostics, diagnostic => diagnostic.Code == "STAGE-ESM-023" && diagnostic.Message == "1 transition bodies analysed.");
        var descriptor = compiled.TypedContextDescriptors.Single();
        var reducerFile = plan.Artifacts.Single(_ => _.RelativePath.EndsWith("Fold.cs", StringComparison.Ordinal));
        const string originalBody = "return new Total(context.Event.Id, context.Event.Amount);";
        foreach (var fixture in AdmittedFixtures)
        {
            var synthetic = await Analyze(fixture);
            var realizedFixture = fixture.Replace("Guid.Empty", "context.Event.Id", StringComparison.Ordinal);
            var files = plan.Artifacts.Where(_ => _.RelativePath.EndsWith(".cs", StringComparison.Ordinal) && _.RelativePath != "Program.cs")
                .Select(artifact => new RenderedFile(artifact.RelativePath,
                    System.Text.Encoding.UTF8.GetString(artifact.Bytes.AsSpan()).Replace(
                        artifact.RelativePath == reducerFile.RelativePath ? originalBody : "\u0000", realizedFixture, StringComparison.Ordinal)));
            var real = RenderedOutput.CreateCompilation(files);
            var actual = PureTransitionAdmission.AnalyzeRendered(real, reducerFile.RelativePath, descriptor);
            Assert.True(synthetic.Accepted == actual.Accepted,
                $"{fixture}: analysis {synthetic.Code}: {synthetic.Reason}; rendered {actual.Code}: {actual.Reason}; " +
                string.Join("; ", real.GetDiagnostics().Where(_ => _.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error)));
            Assert.Equal(synthetic.ContextReads, actual.ContextReads);
            Assert.Equal(synthetic.UsedAllowlistEntries, actual.UsedAllowlistEntries);
            Assert.True(synthetic.Accepted, $"Parity fixture {fixture} was refused: {synthetic.Code}: {synthetic.Reason}");
            Assert.Equal(synthetic.BoundSymbols.ToArray(), actual.BoundSymbols.ToArray());
            Assert.Equal(synthetic.AuditedSymbols.ToArray(), actual.AuditedSymbols.ToArray());
            if (fixture.Contains("foreach (var item in System.Collections.Immutable.ImmutableArray", StringComparison.Ordinal))
            {
                Assert.Contains(synthetic.BoundSymbols, signature => signature.Contains(".GetEnumerator`", StringComparison.Ordinal));
                Assert.Contains(synthetic.BoundSymbols, signature => signature.Contains(".MoveNext`", StringComparison.Ordinal));
                Assert.Contains(synthetic.BoundSymbols, signature => signature.Contains(".Current(", StringComparison.Ordinal));
            }
        }
    }

    [Fact]
    public async Task should_detect_a_planted_changed_member_binding()
    {
        var compiled = await Load(Source.Replace("Guid.Parse(\"00000000-0000-0000-0000-000000000001\")", "Guid.Empty", StringComparison.Ordinal));
        var plan = Plan(compiled);
        Assert.True(plan.Success, string.Join(Environment.NewLine, plan.Diagnostics));
        var descriptor = compiled.TypedContextDescriptors.Single();
        var reducerFile = plan.Artifacts.Single(artifact => artifact.RelativePath.EndsWith("Fold.cs", StringComparison.Ordinal));
        var files = plan.Artifacts.Where(artifact => artifact.RelativePath.EndsWith(".cs", StringComparison.Ordinal) && artifact.RelativePath != "Program.cs")
            .Select(artifact => new RenderedFile(
                artifact.RelativePath,
                System.Text.Encoding.UTF8.GetString(artifact.Bytes.AsSpan()).Replace(
                    artifact.RelativePath == reducerFile.RelativePath ? "return new Total(context.Event.Id, context.Event.Amount);" : "\u0000",
                    "return new Total(context.Event.Id, Math.Abs(-1m));",
                    StringComparison.Ordinal)));
        var intBinding = await Analyze("return new Total(Guid.Empty, Math.Abs(-1));");
        var decimalBinding = PureTransitionAdmission.AnalyzeRendered(RenderedOutput.CreateCompilation(files), reducerFile.RelativePath, descriptor);
        Assert.True(intBinding.Accepted && decimalBinding.Accepted, $"{intBinding.Reason}; {decimalBinding.Reason}");
        Assert.False(intBinding.BoundSymbols.SequenceEqual(decimalBinding.BoundSymbols));
        Assert.Contains(intBinding.BoundSymbols, signature => signature.Contains("Math.Abs`0(None:int)", StringComparison.Ordinal));
        Assert.Contains(decimalBinding.BoundSymbols, signature => signature.Contains("Math.Abs`0(None:decimal)", StringComparison.Ordinal));
    }

    [Fact]
    public async Task should_reject_guid_members_not_in_the_subset()
    {
        var verdict = await Analyze("_ = Guid.Empty; return context.State;", normalizeIdentity: false);
        Assert.Equal("STAGE-ESM-022", verdict.Code);
        Assert.Contains("Guid.Empty", verdict.Reason, StringComparison.Ordinal);
    }

    static async Task<PureTransitionAdmission.Verdict> Analyze(string body, bool normalizeIdentity = true)
    {
        var loaded = await Load(Source.Replace("Guid.Parse(\"00000000-0000-0000-0000-000000000001\")", "Guid.Empty", StringComparison.Ordinal));
        var request = new ArtifactRenderRequest(
            loaded.Model,
            loaded.Plan,
            CratisRendering.CreateProfile("Projects", new("Projects", "Projects")),
            new(ArtifactRenderScopeKind.Application, loaded.Model.Application.Id))
        {
            TypedContextDescriptors = loaded.TypedContextDescriptors
        };
        var context = new SemanticApplicationContext(request, new("Projects", "Projects"));
        var reducer = context.Reducers.Single();
        var transition = reducer.Transitions.Single();
        return PureTransitionAdmission.Analyze(
            (normalizeIdentity ? body.Replace("Guid.Empty", "context.Event.Id", StringComparison.Ordinal) : body)
                .Replace("$WRAPPER$", "TypedContext_" + SemanticTypedContextRenderer.Suffix(loaded.TypedContextDescriptors.Single()), StringComparison.Ordinal),
            context,
            context.ReadModels[reducer.ReadModel],
            context.Events[transition.EventContract],
            loaded.TypedContextDescriptors.Single(),
            loaded.ImplementationRequirements.Single());
    }

    internal static string WithIdentifierConcept(string source) => "concept OrderId : Uuid\n" + source
        .Replace("id Uuid identifier", "id OrderId identifier", StringComparison.Ordinal)
        .Replace("id Uuid", "id OrderId", StringComparison.Ordinal);

    internal static async Task<LoadedSemanticModel> Load(string source)
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, ".git")) && !Directory.Exists(Path.Combine(root.FullName, ".git")))
        {
            root = root.Parent;
        }

        Assert.NotNull(root);
        var folder = Path.Combine(root.FullName, ".ai-work", "stage-namespace", "reducer-fixtures", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            // Tests use the event's generated UUID identity as their inert record key. The submitted
            // reducer body itself is never rewritten by production admission or artifact creation.
            source = source.Replace("Guid.Empty", "context.Event.Id", StringComparison.Ordinal);
            await File.WriteAllTextAsync(Path.Combine(folder, "Orders.play"), source);
            if (source.Contains("file Reducers/Fold.cs", StringComparison.Ordinal))
            {
                Directory.CreateDirectory(Path.Combine(folder, "Reducers"));
                await File.WriteAllTextAsync(Path.Combine(folder, "Reducers", "Fold.cs"), "return new Total(context.Event.Id, context.Event.Amount);");
            }

            return await SemanticModelLoader.LoadFromPathAsync(folder, null, "Projects");
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    internal static ArtifactRenderPlan Plan(LoadedSemanticModel compiled, ArtifactRenderScope? scope = null)
    {
        var model = compiled.Model;
        var request = new ArtifactRenderRequest(
            model,
            SemanticExecutionPlan.Compile(model).Plan,
            CratisRendering.CreateProfile("Projects", new("Projects", "Projects")),
            scope ?? new(ArtifactRenderScopeKind.Application, model.Application.Id))
        {
            ImplementationRequirements = compiled.ImplementationRequirements,
            ImplementationContents = compiled.ImplementationContents,
            TypedContextDescriptors = compiled.TypedContextDescriptors
        };
        return new CratisArtifactRenderPlanner().Plan(request);
    }
}
#endif
