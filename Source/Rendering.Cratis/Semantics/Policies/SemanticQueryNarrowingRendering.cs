// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Naming;

namespace Cratis.Stage.Rendering.Cratis.Semantics.Policies;

/// <summary>
/// Renders a collection query that narrows its rows by a policy (ESM v47) as an Arc row narrowing.
/// </summary>
/// <remarks>
/// The generated narrowing decides each row of the read model with the policy's reads keyed by the row's own properties,
/// and keeps the keys of the rows the policy allows. Arc turns those keys into a predicate it applies before sorting, paging
/// and counting, so a page is never short and a total is never wrong. A row whose state is absent, unreadable or rejected is dropped.
/// </remarks>
internal static class SemanticQueryNarrowingRendering
{
    internal static IEnumerable<(LocatedSemanticSlice Located, SemanticKeyedQuery Query, SemanticPolicy Policy, SemanticPolicyReference Reference)> Narrowed(
        SemanticApplicationContext context,
        IEnumerable<LocatedSemanticSlice> slices) =>
        slices.SelectMany(located => located.Slice.Queries
            .Where(query => query.Narrowing is not null)
            .Select(query => (Located: located, Query: query, Reference: query.Narrowing!, Policy: context.Application.Policies.Single(policy => policy.Name == query.Narrowing!.Name)))
            .Select(item => (item.Located, item.Query, item.Policy, item.Reference)));

    internal static bool Admits(SemanticApplicationContext context, SemanticKeyedQuery query, List<ArtifactRenderDiagnostic> diagnostics)
    {
        if (query.Narrowing is not { } reference)
        {
            return true;
        }

        var policy = context.Application.Policies.Single(candidate => candidate.Name == reference.Name);
        if (!context.ReadModels.TryGetValue(query.ReadModel, out var model) || model.Properties.FirstOrDefault(property => property.IsIdentifier) is null)
        {
            diagnostics.Add(Error($"Query '{query.Name}' narrows rows of a read model that has no key to return them by.", query.Id));
            return false;
        }

        var scope = SemanticStatePolicyRendering.Scope(policy, reference, context);
        var admitted = true;
        foreach (var read in policy.Reads)
        {
            var source = SemanticStatePolicyRendering.Source(reference, read);
            if (model.Properties.FirstOrDefault(property => property.Name == source) is not { Type.IsCollection: false } property ||
                SemanticClaimTargets.Primitive(context, property) is not (SemanticPrimitiveType.Text or SemanticPrimitiveType.Uuid or SemanticPrimitiveType.Date or SemanticPrimitiveType.DateTime))
            {
                diagnostics.Add(Error($"Query '{query.Name}' narrows by policy '{policy.Name}', whose read '{read.Name}' takes its key from '{source}', which is not a text, Uuid, Date or DateTime property of '{model.Name}'.", query.Id));
                admitted = false;
            }
        }

        foreach (var state in SemanticStatePolicyRendering.Leaves(policy.Condition).Where(state => !SemanticStatePolicyRendering.CanRender(state, context, scope)))
        {
            diagnostics.Add(Error($"Policy '{policy.Name}' used to narrow '{query.Name}' has a state condition Stage cannot render.", query.Id));
            admitted = false;
        }

        if (!AllConditionsAreStateOrCaller(policy.Condition))
        {
            diagnostics.Add(Error($"Policy '{policy.Name}' used to narrow '{query.Name}' has a condition that cannot be evaluated per row.", query.Id));
            admitted = false;
        }

        return admitted;
    }

    internal static IEnumerable<RenderedFile> Render(SemanticApplicationContext context, IReadOnlyList<LocatedSemanticSlice> slices)
    {
        var types = new SemanticTypeSystem(context);
        foreach (var (located, query, policy, reference) in Narrowed(context, slices))
        {
            var model = context.ReadModels[query.ReadModel];
            var key = model.Properties.First(property => property.IsIdentifier);
            var typeName = types.SliceType(model.Id, model.Name);
            var className = $"{Identifiers.ToPascalCase(query.Name)}Narrowing";
            var keyType = types.Type(key.Type);
            var keyMember = context.MemberNames.Property(key);
            var scope = SemanticStatePolicyRendering.Scope(policy, reference, context);
            var properties = model.Properties;
            var builder = context.CodeBuilder()
                .Namespace(context.PoliciesNamespace)
                .Using("Cratis.Arc.Queries");
            builder.Summary($"Narrows the rows of the query '{query.Name}' to those the policy '{policy.Name}' allows for the recorded state.")
                .OpenBlock($"public sealed class {className}(global::Cratis.Chronicle.ReadModels.IReadModels readModels) : global::Cratis.Arc.Queries.QueryRowNarrowing<{typeName}, {keyType}>")
                .Line("/// <inheritdoc/>")
                .Line($"protected override global::System.Linq.Expressions.Expression<global::System.Func<{typeName}, {keyType}>> Key => row => row.{keyMember};")
                .BlankLine()
                .Line("/// <inheritdoc/>")
                .Line($"protected override bool AppliesTo(global::Cratis.Arc.Queries.QueryContext context) => context.Name.Value.EndsWith(\".{Identifiers.ToPascalCase(query.Name)}\", global::System.StringComparison.Ordinal);")
                .BlankLine()
                .Line("/// <inheritdoc/>")
                .OpenBlock($"protected override async global::System.Threading.Tasks.ValueTask<global::System.Collections.Generic.IEnumerable<{keyType}>> GetAllowedKeys(global::Cratis.Arc.Queries.QueryContext context)")
                .Line($"var allowed = new global::System.Collections.Generic.List<{keyType}>();")
                .Line($"foreach (var row in await readModels.GetInstances<{typeName}>())")
                .OpenBlock(string.Empty)
                .Line("#pragma warning disable CA1031 // A failing read drops the row: narrowing fails closed.");
            var available = SemanticStatePolicyRendering.Available(reference);
            builder.Line($"var {available} = true;");
            foreach (var read in policy.Reads)
            {
                var view = context.ReadModels[read.ReadModel];
                var type = types.SliceType(view.Id, view.Name);
                var local = scope.Local(read.Name);
                var source = SemanticStatePolicyRendering.Source(reference, read);
                var sourceProperty = properties.First(property => property.Name == source);
                var rowKey = $"(global::Cratis.Chronicle.Events.EventSourceId)row.{context.MemberNames.Property(sourceProperty)}";
                builder.Line($"{type} {local} = null!;")
                    .OpenBlock("try")
                    .Line($"{local} = await readModels.GetInstanceById<{type}>({rowKey});")
                    .EndBlock()
                    .OpenBlock("catch (global::System.Exception exception) when (!(exception is global::System.OperationCanceledException && context.CancellationToken.IsCancellationRequested))")
                    .Line($"{available} = false;")
                    .EndBlock()
                    .Line($"if (global::System.Object.ReferenceEquals({local}, null)) {available} = false;");
            }

            var condition = SemanticPolicyArtifactRenderer.Condition(policy.Condition, context, [], command: false, string.Empty, string.Empty, scope);
            builder.Line("#pragma warning restore CA1031")
                .Line($"if ({available} && {condition}) allowed.Add(row.{keyMember});")
                .EndBlock()
                .Line("return allowed;")
                .EndBlock()
                .BlankLine()
                .Line("// Module initialization registers the narrowing with the services, without assembly scanning.")
                .Line("#pragma warning disable CA2255 // Generated application registration is intentionally initialized at module load.")
                .Line("[global::System.Runtime.CompilerServices.ModuleInitializer]")
                .OpenBlock("internal static void InitializeGeneratedNarrowing()")
                .Line($"{(context.Domain.Count == 0 ? "Registration" : $"global::{context.RootNamespace}.GeneratedPolicies.Registration")}.Add({CSharpCodeBuilder.StringLiteral("narrowing:" + className)}, services => global::Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddScoped<global::Cratis.Arc.Queries.IQueryRowNarrowing, {className}>(services));")
                .EndBlock()
                .Line("#pragma warning restore CA2255")
                .EndBlock();
            yield return new(Path.Combine(context.PoliciesFolder, $"{className}.cs"), builder.ToString());
        }
    }

    static bool AllConditionsAreStateOrCaller(SemanticPolicyCondition condition) => condition switch
    {
        SemanticStatePolicyCondition or SemanticAuthenticatedCondition => true,
        SemanticNotPolicyCondition not => AllConditionsAreStateOrCaller(not.Operand),
        SemanticLogicalPolicyCondition logical => AllConditionsAreStateOrCaller(logical.Left) && AllConditionsAreStateOrCaller(logical.Right),
        _ => false
    };

    static ArtifactRenderDiagnostic Error(string message, SemanticId artifact) =>
        new("STAGE-ESM-016", ArtifactRenderDiagnosticSeverity.Error, message, artifact);
}
