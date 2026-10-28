// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Runtime.CompilerServices;
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Contracts.Semantics;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_inventorying_reducer_typed_contexts;
using Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticSpecificationArtifactRenderer.when_rendering_reaction_cascades.given;
using Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticSpecificationAdmission.when_validating_append_refusals;
using Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticReactionSpecificationRenderer.given;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;

/// <summary>
/// The single list of render requests that the generated-type inventory guard and the byte pin both walk: the
/// corpus source forms, one fixture per specification kind and one per typed-context kind.
/// </summary>
internal static class inventory_fixtures
{
    /// <summary>
    /// The names of the typed-context fixtures, each of which must emit at least one typed-context declaration.
    /// </summary>
    internal static readonly string[] TypedContextNames = ["reducer typed contexts", "validation typed contexts", "policy typed contexts"];

    const string QuerySource = """
        concept ProjectId : Uuid
        module Projects
          feature Registration
            slice StateChange Register
              command RegisterProject
                projectId ProjectId identifier
                name String
                produces ProjectRegistered
                  for projectId
                  projectId = projectId
                  name = name
              event ProjectRegistered
                projectId ProjectId
                name String
            slice StateView Lookup
              readmodel ProjectSummary
                projectId ProjectId
                name String
              query ById => ProjectSummary?
                by projectId ProjectId
              projection Summary => ProjectSummary
                from ProjectRegistered key projectId
                  name = name
              specification LookingUp
                given ProjectRegistered
                  for "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                  projectId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                  name = "Stage"
                then query ById
                  arguments
                    projectId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                  result
                    projectId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                    name = "Stage"
        """;

    const string PolicyBody = "return context.Identity.HasRole(\"Clerk\") && context.Occurred.Year == 2027 && context.Occurred.Month == 3 && context.Occurred.Day == 4;";
    const string ValidationBody = "return context.Value > 0 && context.Artifact.Amount == context.Value && context.Property == \"amount\" && !context.IsWholeArtifact;";

    static readonly CratisRenderingOptions _invoiceOptions = new("InvoiceApp", "Invoices");

    internal static IEnumerable<(string Name, ArtifactRenderRequest Request, CratisRenderingOptions Options)> All()
    {
        var projects = new CratisRenderingOptions("Projects", "Projects");
        foreach (var form in RegisterProjectCorpus.LegacyV1.SourceForms)
        {
            yield return (form.Name, Application(Compile(form), projects), projects);
        }

        yield return ("composite keys", Application(Composite(), projects), projects);
        yield return ("strings", Strings(projects), projects);

        var options = new CratisRenderingOptions("InvoiceApp", "Invoices");
        foreach (var (name, source) in SpecificationSources())
        {
            yield return (name, Application(invoice_model.Compile(source), options), options);
        }

        var inventory = new CratisRenderingOptions("Inventory", "Inventory");
        yield return ("cascade with trigger", Application(Semantics.for_SemanticAutomationArtifactRenderer.given.an_automation.Compile(an_inventory.OneSlice), inventory), inventory);
        yield return ("append refusal", Application(invoice_model.Compile(with_an_append_a_unique_event_constraint_refuses.Source), options), options);
        yield return ("translation specification", Application(Translation(), new("Contracts", "Contracts")), new("Contracts", "Contracts"));

        var reducer = reducer_context_model.LoadMinA3(ReducerFixturePath()).GetAwaiter().GetResult();
        yield return (TypedContextNames[0], reducer_context_model.Request(reducer), reducer_context_model.Options);

        var validation = opaque_policy_model.Load(validation_body_model.Command(ValidationBody, generated: true));
        yield return (TypedContextNames[1], Loaded(validation), _invoiceOptions);

        var policy = opaque_policy_model.Load(opaque_policy_model.Source(PolicyBody));
        yield return (TypedContextNames[2], Loaded(policy), _invoiceOptions);
    }

    /// <summary>
    /// Builds a context the way the planner does after admission: the refused specifications are assigned, so the inventory names exactly the rendered ones.
    /// </summary>
    /// <param name="request">The render request.</param>
    /// <param name="options">The rendering options.</param>
    /// <returns>The admitted context.</returns>
    internal static SemanticApplicationContext Admitted(ArtifactRenderRequest request, CratisRenderingOptions options)
    {
        var context = new SemanticApplicationContext(request, options);
        context.ScopeFlatProjections();
        var diagnostics = new List<ArtifactRenderDiagnostic>();
        SemanticCratisAdmission.Evaluate(context, context.SelectedSlices(), diagnostics);
        context.RefusedSpecifications = [.. SemanticRefusalScope.Degrade(context, diagnostics).Select(refusal => refusal.Artifact)];
        return context;
    }

    static string ReducerFixturePath([CallerFilePath] string path = "") =>
        Path.Combine(Path.GetDirectoryName(path)!, "..", "when_inventorying_reducer_typed_contexts", "reducer_context_model.cs");

    static IEnumerable<(string Name, string Source)> SpecificationSources()
    {
        yield return ("scoped projections", when_rendering_scoped_projections.ScopedSource);
        yield return ("query", QuerySource);
        yield return ("denied query", DeniedQuerySource());
        yield return ("reaction production", a_reaction_specification.ProductionSource);
        yield return ("redelivery", a_redelivery_specification.RedeliverySource);
        yield return ("case table", a_specification_case_table.Source);
    }

    static ArtifactRenderRequest Application(ExecutableSemanticModel model, CratisRenderingOptions options) =>
        new(model, SemanticExecutionPlan.Compile(model).Plan!, CratisRendering.CreateProfile(model.Application.Name, options), new(ArtifactRenderScopeKind.Application, model.Application.Id));

    static ArtifactRenderRequest Loaded(LoadedSemanticModel loaded) =>
        new(loaded.Model, loaded.Plan, CratisRendering.CreateProfile(loaded.Model.Application.Name, _invoiceOptions), new(ArtifactRenderScopeKind.Application, loaded.Model.Application.Id))
        {
            ImplementationRequirements = loaded.ImplementationRequirements,
            ImplementationContents = loaded.ImplementationContents,
            TypedContextDescriptors = loaded.TypedContextDescriptors
        };

    static ExecutableSemanticModel Compile(CanonicalCorpusSourceForm form)
    {
        var catalog = SemanticIdentityCatalogSerializer.Deserialize(form.IdentityCatalogBytes.AsSpan());
        var documents = form.Documents.Select(document => SemanticSourceDocument.Create(
            catalog.ResolveDocument(document.StableKey),
            document.StableKey,
            document.DisplayPath,
            document.Text));
        var compilation = new SemanticModelCompiler().Compile(
            RegisterProjectCorpus.LegacyV1.ApplicationName,
            SemanticDocumentSet.Create([.. documents], catalog));
        Xunit.Assert.True(compilation.Success, string.Join(Environment.NewLine, compilation.Diagnostics));
        return compilation.Value!.Model;
    }

    static ArtifactRenderRequest Strings(CratisRenderingOptions options)
    {
        var documented = when_rendering_authoring_documentation.Compile(when_rendering_authoring_documentation.Source).Model;
        var profile = CratisRendering.CreateProfile(documented.Application.Name, options, scene: null, stringsFiles: new Dictionary<string, string> { ["projects.en.strings"] = "project.name = \"Project name\"\n" }, defaultLocale: "en");
        return new(documented, SemanticExecutionPlan.Compile(documented).Plan!, profile, new(ArtifactRenderScopeKind.Application, documented.Application.Id));
    }

    static ExecutableSemanticModel Composite()
    {
        var composite = SemanticModelSerializer.Deserialize(CompositeReadModelKeysCorpus.V27.EsmBytes.AsSpan());
        var module = composite.Application.Modules.Single();
        var feature = module.Features.Single();
        var slice = feature.Slices.Single();
        slice = slice with { Specifications = [slice.Specifications.Single(specification => specification.Name == "RecreateRemovedParts")], Queries = [] };
        return ExecutableSemanticModel.Create(composite.LanguageVersion, composite.SemanticVersion, composite.Application with { Modules = [module with { Features = [feature with { Slices = [slice] }] }] });
    }

    static ExecutableSemanticModel Translation()
    {
        var model = SemanticModelSerializer.Deserialize(PublicEventsCorpus.V9.EsmBytes.ToArray());
        var orderId = new SemanticTextValue("3fa85f64-5717-4562-b3fc-2c963f66afa6");
        var module = model.Application.Modules.Single();
        var feature = module.Features.Single();
        var slices = feature.Slices.Select(slice =>
        {
            if (slice.Name != "TrackShipments") return slice with { Specifications = [] };
            var source = slice.Events.Single(contract => contract.Name == "ShipmentDispatched");
            var target = slice.Events.Single(contract => contract.Name == "OrderDispatched");
            var property = source.Properties.Single();
            return slice with
            {
                Specifications = [new(
                    SemanticId.Create(SemanticKind.Specification, "receive-shipment"),
                    "ReceivingAShipment",
                    [],
                    [],
                    null,
                    [new(target.Id, [new(target.Properties.Single().Id, orderId)]) { EventSource = new(SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text), orderId) }],
                    [],
                    [],
                    [])
                {
                    WhenAppended = new(source.Id, [new(property.Id, orderId)])
                }]
            };
        });
        return ExecutableSemanticModel.Create(model.LanguageVersion, model.SemanticVersion, model.Application with { Modules = [module with { Features = [feature with { Slices = [.. slices] }] }] });
    }

    static string DeniedQuerySource() => "policy Clerks\n  require authenticated\n" + QuerySource
        .Replace("query ById => ProjectSummary?\n", "query ById => ProjectSummary?\n        authorize Clerks\n", StringComparison.Ordinal)
        .Replace("      specification LookingUp\n", "      specification LookingUp\n        given caller\n", StringComparison.Ordinal)
        .Replace("        given ProjectRegistered\n          for \"3fa85f64-5717-4562-b3fc-2c963f66afa6\"\n          projectId = \"3fa85f64-5717-4562-b3fc-2c963f66afa6\"\n          name = \"Stage\"\n", string.Empty, StringComparison.Ordinal)
        .Replace("          result\n            projectId = \"3fa85f64-5717-4562-b3fc-2c963f66afa6\"\n            name = \"Stage\"", "        then denied", StringComparison.Ordinal);
}
#endif
