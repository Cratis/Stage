// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Cratis.Screenplay;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using Cratis.Stage.Rendering.Cratis.Naming;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_SemanticTypedContextRenderer;

public class when_generating_packaged_vectors
{
    // The frozen files in Vectors/ are Screenplay v4.41.0's typed-contexts-v1 and
    // unbound-handler-context-v1 canonical vectors; compare every member, not a sample.
    const string Source = """
        concept Code : String
          validate
            rule ValidCode
              file Rules/ValidCode.cs
          validate csharp
            ```csharp
            return true;
            ```
        policy Access
          ```csharp
          return true;
          ```
        policy Unused
          ```csharp
          return true;
          ```
        module Billing
          feature Accounts
            slice StateView Balances
              event Deposited
                amount Decimal
              event Deposited generation 2
                current String
              event Withdrawn
                reason String
              readmodel Balance
                id Uuid
              reducer Fold => Balance
                on Deposited
                  file Reducers/Deposited.cs
                on Withdrawn
                  file Reducers/Withdrawn.cs
              query ById => Balance?
                by id Uuid
                authorize Access
            slice StateChange Commands
              command Deposit
                code Code
                authorize Access
                validate csharp
                  ```csharp
                  return true;
                  ```
                validate
                  code rule ValidCode
                    ```csharp
                    return true;
                    ```
        """;

    [Fact]
    public void should_pin_every_type_member_and_nullability_to_the_bound_vector()
    {
        var compilation = Bind(Source);
        Assert.True(compilation.Success);
        using var vector = JsonDocument.Parse(Vector("typed-contexts-v1.json"));
        Assert.Equal(Vector("typed-contexts-v1.json"), SemanticTypedContextSerializer.Serialize(compilation.TypedContextDescriptors));
        var descriptors = vector.RootElement.GetProperty("descriptors").EnumerateArray().ToArray();
        Assert.Equal(descriptors.Length, compilation.TypedContextDescriptors.Length);
        var context = Context(compilation.Value!.Model);
        for (var index = 0; index < descriptors.Length; index++)
        {
            var descriptor = compilation.TypedContextDescriptors[index];
            var generated = SemanticTypedContextRenderer.Render(descriptor, context).Content;
            Assert.DoesNotContain("dynamic", generated, StringComparison.Ordinal);
            var members = descriptors[index].GetProperty("members").EnumerateArray().ToArray();
            Assert.Equal(members.Length, descriptor.Members.Length);
            foreach (var (expected, actual) in members.Zip(descriptor.Members))
            {
                Assert.Equal(expected.GetProperty("name").GetString(), actual.Name);
                Assert.Equal(expected.GetProperty("isNullable").GetBoolean(), actual.IsNullable);
                Assert.Equal(expected.GetProperty("isDerived").GetBoolean(), actual.IsDerived);
                Assert.Equal(expected.GetProperty("type").GetProperty("kind").GetString(), actual.Type.Kind);
                Assert.Equal(expected.GetProperty("source").GetProperty("kind").GetString(), actual.Source.Kind);
                var type = ExpectedType(expected.GetProperty("type"), descriptor, context);
                if (actual.IsNullable && !type.EndsWith('?')) type += "?";
                if (actual.IsDerived)
                    Assert.Contains($"public {type} {actual.Name} =>", generated, StringComparison.Ordinal);
                else
                    Assert.Contains($"{type} {actual.Name}", generated, StringComparison.Ordinal);
                foreach (var property in expected.GetProperty("type").GetProperty("properties").EnumerateArray())
                {
                    var resolved = actual.Type.Properties.Single(item => item.Name == property.GetProperty("name").GetString());
                    Assert.Equal(property.GetProperty("id").GetString(), resolved.Id.ToString());
                    Assert.Equal(property.GetProperty("type").GetProperty("isOptional").GetBoolean(), resolved.Type.IsOptional);
                    Assert.Equal(property.GetProperty("type").GetProperty("isCollection").GetBoolean(), resolved.Type.IsCollection);
                }
            }
        }
    }

    [Fact]
    public void should_compile_all_vector_wrappers_against_generated_model_types()
    {
        var compilation = Bind(Source);
        var context = Context(compilation.Value!.Model);
        var wrappers = compilation.TypedContextDescriptors.Select(descriptor => SemanticTypedContextRenderer.Render(descriptor, context));
        var declarations = new RenderedFile("Model.cs", """
            namespace Projects.Common { public record Code(string Value); }
            namespace Projects.Billing.Accounts.Commands { public record Deposit; }
            namespace Projects.Billing.Accounts.Balances
            {
                public record Balance;
                public record Deposited;
                public record Withdrawn;
            }
            """);
        var errors = RenderedOutput.Errors([declarations, .. wrappers]);
        Assert.True(errors.Count == 0, string.Join(Environment.NewLine, errors));
    }

    [Fact]
    public void should_preserve_an_optional_rule_value_without_making_its_artifact_nullable()
    {
        var compilation = Bind("""
            module Billing
              feature Accounts
                slice StateChange Commands
                  command Deposit
                    note String?
                    validate
                      note rule Check
                        ```csharp
                        return true;
                        ```
            """);
        Assert.True(compilation.Success);
        var descriptor = compilation.TypedContextDescriptors.Single(item => item.Role == SemanticImplementationRole.RulePredicate);
        var source = SemanticTypedContextRenderer.Render(descriptor, Context(compilation.Value!.Model)).Content;
        Assert.Contains("string? Value", source, StringComparison.Ordinal);
        Assert.Contains(".Deposit Artifact", source, StringComparison.Ordinal);
        Assert.DoesNotContain(".Deposit? Artifact", source, StringComparison.Ordinal);
    }

    [Fact]
    public void should_refuse_unknown_tokens_kinds_sources_and_revisions_before_reducer_admission()
    {
        var compilation = Bind(Source);
        var descriptor = compilation.TypedContextDescriptors[0];
        var model = compilation.Value!.Model;
        var context = Context(model);
        var member = descriptor.Members[0];
        var unknownToken = descriptor with
        {
            Members = descriptor.Members.SetItem(0, member with
            {
                Type = new(SemanticContextTypeKinds.Runtime, null, null, "NewRuntimeToken")
            })
        };
        Assert.Throws<InvalidTypedContext>(() => SemanticTypedContextRenderer.Render(unknownToken, context));
        Assert.Throws<InvalidTypedContext>(() => SemanticTypedContextRenderer.Render(
            descriptor with { Members = descriptor.Members.SetItem(0, member with { Type = member.Type with { Kind = "unknown" } }) },
            context));
        Assert.Throws<InvalidTypedContext>(() => SemanticTypedContextRenderer.Render(
            descriptor with { Members = descriptor.Members.SetItem(0, member with { Source = member.Source with { Kind = "unknown" } }) },
            context));

        var plan = SemanticExecutionPlan.Compile(model).Plan!;
        var request = new ArtifactRenderRequest(
            model,
            plan,
            CratisRendering.CreateProfile("Projects", new("Projects", "Projects")),
            new(ArtifactRenderScopeKind.Application, model.Application.Id))
        {
            ImplementationRequirements = compilation.ImplementationRequirements,
            TypedContextDescriptors = [descriptor],
            TypedContextContractRevision = 2
        };

        // ESM v4 is checked before the descriptor envelope, even if its revision is invalid.
        Assert.Contains(new CratisArtifactRenderPlanner().Plan(request).Diagnostics, diagnostic => diagnostic.Code == "STAGE-ESM-016");
        Assert.Contains(Plan(model, compilation.ImplementationRequirements, [descriptor], 2).Diagnostics, diagnostic =>
            diagnostic.Code == "STAGE-ESM-021" && diagnostic.Message.Contains("revision '2'", StringComparison.Ordinal));
    }

    [Fact]
    public void should_pin_the_supported_contract_revision()
    {
        Assert.Equal(1u, SemanticTypedContextDescriptor.ContractRevision);
        Assert.Equal(1u, SemanticTypedContextAdmission.SupportedTypedContextContractRevision);
    }

    [Fact]
    public void should_refuse_the_unbound_handler_vector_through_stage()
    {
        const string unbound = """
            module Billing
              feature Invoices
                slice StateChange ProcessBatch
                  command ProcessBatch
                    id Uuid identifier
                    handler
                      csharp
                        ```
                        return new object[] { context.Identity.Id };
                        ```
            """;
        var failed = Compile(unbound);
        Assert.False(failed.Success);
        Assert.Contains(failed.TypedContextDescriptors, descriptor => !descriptor.IsWrapperReady && descriptor.Role == SemanticImplementationRole.CommandHandler);
        var valid = Bind(Source);
        var model = valid.Value!.Model;
        Assert.Contains(Plan(model, failed.ImplementationRequirements, failed.TypedContextDescriptors).Diagnostics,
            diagnostic => diagnostic.Code == "STAGE-ESM-021");
    }

    [Fact]
    public void should_refuse_a_descriptor_from_a_different_model_revision()
    {
        var compilation = Bind(Source);
        var model = compilation.Value!.Model;
        var changed = ExecutableSemanticModel.Create(
            model.LanguageVersion,
            model.SemanticVersion,
            model.Application with { Name = "ChangedProjects" });
        Assert.NotEqual(model.Revision, changed.Revision);
        Assert.Contains(Plan(changed, compilation.ImplementationRequirements, [compilation.TypedContextDescriptors[0]]).Diagnostics,
            diagnostic => diagnostic.Code == "STAGE-ESM-021" && diagnostic.Message.Contains("revision", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void should_refuse_a_role_or_context_version_mismatch(bool changeRole)
    {
        var compilation = Bind(Source);
        var descriptor = compilation.TypedContextDescriptors[0];
        var changed = changeRole
            ? descriptor with { Role = SemanticImplementationRole.PolicyPredicate }
            : descriptor with { ContextVersion = 2 };
        Assert.Contains(Plan(compilation.Value!.Model, compilation.ImplementationRequirements, [changed]).Diagnostics,
            diagnostic => diagnostic.Code == "STAGE-ESM-021");
    }

    [Fact]
    public void should_refuse_missing_arrays_and_duplicate_type_ids_or_query_shapes()
    {
        var compilation = Bind(Source);
        var model = compilation.Value!.Model;
        var descriptor = compilation.TypedContextDescriptors[0];
        var malformed = new[]
        {
            descriptor with { Types = default },
            descriptor with { Members = default },
            descriptor with { Types = [.. descriptor.Types, .. descriptor.Types] }
        };
        foreach (var item in malformed)
        {
            Assert.Contains(Plan(model, compilation.ImplementationRequirements, [item]).Diagnostics,
                diagnostic => diagnostic.Code == "STAGE-ESM-021" && diagnostic.Message.Contains("missing arrays", StringComparison.Ordinal));
        }

        var query = compilation.TypedContextDescriptors.Single(item => item.Role == SemanticImplementationRole.PolicyPredicate &&
            item.OperationId == model.Application.Modules.Single().Features.Single().Slices.Single(slice => !slice.Queries.IsEmpty).Queries.Single().Id);
        var artifact = query.Members.Single(member => member.Name == "Artifact");
        var duplicateShape = query with { Members = query.Members.Add(artifact) };
        Assert.Contains(Plan(model, compilation.ImplementationRequirements, [duplicateShape]).Diagnostics,
            diagnostic => diagnostic.Code == "STAGE-ESM-021" && diagnostic.Message.Contains("duplicate query shapes", StringComparison.Ordinal));
    }

    static ArtifactRenderPlan Plan(
        ExecutableSemanticModel model,
        ImmutableArray<SemanticImplementationRequirement> requirements,
        ImmutableArray<SemanticTypedContextDescriptor> descriptors,
        uint revision = 1)
    {
        var modules = model.Application.Modules.Select(module => module with
        {
            Features = [.. module.Features.Select(feature => feature with
            {
                Slices = [.. feature.Slices.Select(slice => slice with
                {
                    Events = [.. slice.Events.Select(@event => @event with
                    {
                        Revision = EventContractRevision.Initial,
                        Predecessor = null,
                        PriorRevisions = []
                    })]
                })]
            })]
        });
        model = ExecutableSemanticModel.Create(LanguageVersion.V3, SemanticVersion.V3, model.Application with { Modules = [.. modules] });
        var request = new ArtifactRenderRequest(
            model,
            SemanticExecutionPlan.Compile(model).Plan!,
            CratisRendering.CreateProfile(model.Application.Name, new("Projects", "Projects")),
            new(ArtifactRenderScopeKind.Application, model.Application.Id))
        {
            ImplementationRequirements = requirements,
            TypedContextDescriptors = descriptors,
            TypedContextContractRevision = revision
        };
        return new CratisArtifactRenderPlanner().Plan(request);
    }

    static CompilationResult<SemanticCompilation> Compile(string source)
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("application-document"), "application-document", "application.play", source);
        return new SemanticModelCompiler().Compile("Projects", SemanticDocumentSet.Create([document], catalog));
    }

    static byte[] Vector(string name)
    {
        using var stream = typeof(when_generating_packaged_vectors).Assembly.GetManifestResourceStream(
            $"Cratis.Stage.Rendering.Cratis.for_SemanticTypedContextRenderer.Vectors.{name}")!;
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        return buffer.ToArray();
    }

    static CompilationResult<SemanticCompilation> Bind(string source)
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects"));
        const string key = "application-document";
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument(key), key, "application.play", source);
        return new SemanticModelBinder().Bind(
            "Projects", new ScreenplayCompiler().Parse(source).Value!, SemanticDocumentSet.Create([document], catalog));
    }

    static SemanticApplicationContext Context(ExecutableSemanticModel model)
    {
        var request = new ArtifactRenderRequest(
            model,
            SemanticExecutionPlan.Compile(model).Plan!,
            CratisRendering.CreateProfile("Projects", new("Projects", "Projects")),
            new(ArtifactRenderScopeKind.Application, model.Application.Id));
        return new(request, new("Projects", "Projects"));
    }

    static string ExpectedType(JsonElement type, SemanticTypedContextDescriptor descriptor, SemanticApplicationContext context)
    {
        switch (type.GetProperty("kind").GetString())
        {
            case SemanticContextTypeKinds.Runtime:
                return type.GetProperty("runtimeToken").GetString() switch
                {
                    "Text" => "string", "WholeNumber" => "long", "Boolean" => "bool", "DateTime" => "DateTimeOffset",
                    "TenantId" => "global::Cratis.Screenplay.Contexts.TenantId",
                    "Identity" => "global::Cratis.Screenplay.Contexts.Identity",
                    "CausedBy" => "global::Cratis.Screenplay.Contexts.CausedBy",
                    "Causation" => "global::Cratis.Screenplay.Contexts.Causation",
                    _ => throw new InvalidOperationException("Vector introduced a new token")
                };
            case SemanticContextTypeKinds.Shape:
                var id = type.GetProperty("shape").GetString();
                var shape = descriptor.Members.First(member => member.Type.Shape?.ToString() == id).Type.Shape!.Value;
                if (context.Queries.ContainsKey(shape))
                {
                    var suffix = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{descriptor.RequirementId}:{descriptor.OperationId}")))[..16];
                    return $"TypedContext_{suffix}_ArtifactShape";
                }
                var located = context.DeclaringSlice(shape);
                string name;
                if (context.Commands.TryGetValue(shape, out var command)) name = command.Name;
                else if (context.Events.TryGetValue(shape, out var @event)) name = @event.Name;
                else name = context.ReadModels[shape].Name;
                return $"global::{SliceNaming.Namespace(context.RootNamespace, located.Path)}.{Identifiers.ToPascalCase(name)}";
            case SemanticContextTypeKinds.Model:
                var modelType = type.GetProperty("modelType");
                var primitive = modelType.GetProperty("primitive").GetString();
                var scalar = modelType.GetProperty("kind").GetString() switch
                {
                    "Primitive" => primitive switch
                    {
                        "Uuid" => "Guid", "Text" => "string", "WholeNumber" => "int", "DecimalNumber" => "decimal",
                        "Boolean" => "bool", "Date" => "DateOnly", "DateTime" => "DateTimeOffset", _ => throw new InvalidOperationException()
                    },
                    "Concept" or "CompositeType" => $"global::Projects.Common.{Identifiers.ToPascalCase(descriptor.Types.Single(def => def.Id.ToString() == modelType.GetProperty("target").GetString()).Name)}",
                    _ => throw new InvalidOperationException()
                };
                if (modelType.GetProperty("isCollection").GetBoolean()) scalar = $"IReadOnlyList<{scalar}>";
                return modelType.GetProperty("isOptional").GetBoolean() ? $"{scalar}?" : scalar;
            default:
                throw new InvalidOperationException("Vector introduced a new type kind");
        }
    }
}
#endif
