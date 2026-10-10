// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Naming;

namespace Cratis.Stage.Rendering.Cratis.Semantics.Policies;

/// <summary>
/// Renders one scoped Arc policy for each protected operation and its exact effective authorization expression.
/// </summary>
internal static class SemanticPolicyArtifactRenderer
{
    public static string Name(SemanticId id) => $"StagePolicy_{id.ToString().Replace('-', '_').Replace(':', '_')}";

    public static string PolicyName(SemanticApplicationContext context, SemanticId id) =>
        string.Join('.', context.Domain.Append(Name(id)));

    public static IEnumerable<RenderedFile> Render(SemanticApplicationContext context, IReadOnlyList<LocatedSemanticSlice> slices)
    {
        yield return RenderShared(context);
        var operations = slices.SelectMany(slice => slice.Slice.Commands.Where(command => command.Authorization is not null)
                .Select(command => (command.Id, Authorization: command.Authorization!, IsCommand: true, Argument: string.Empty, Subject: command.Properties.Single(property => property.IsIdentifier).Name, Properties: (IReadOnlyList<SemanticProperty>)command.Properties)))
            .Concat(slices.SelectMany(slice => slice.Slice.Queries.Where(query => query.Authorization is not null)
                .Select(query =>
                {
                    var argument = query.Argument;
                    return (query.Id, Authorization: query.Authorization!, IsCommand: false,
                        Argument: argument is null ? string.Empty : Identifiers.ToCamelCase(argument.Name),
                        Subject: argument?.Name ?? string.Empty,
                        Properties: (IReadOnlyList<SemanticProperty>)(argument is null ? [] : [new(argument.Id, argument.Name, argument.Type, false)]));
                })))
            .OrderBy(operation => operation.Id.ToString(), StringComparer.Ordinal).ToArray();
        foreach (var operation in operations)
        {
            var builder = new CSharpCodeBuilder()
                .Namespace(context.PoliciesNamespace)
                .Using("Cratis.Arc.Authorization");
            if (context.Domain.Count > 0)
            {
                builder.Using($"{context.RootNamespace}.GeneratedPolicies");
            }
            var expression = Authorization(operation.Authorization, context, operation.Properties, operation.IsCommand, operation.Argument, operation.Subject, operation.Id);
            const string signature = "public global::System.Threading.Tasks.ValueTask<bool> IsAuthorized(global::Cratis.Arc.Authorization.AuthorizationPolicyContext context, global::System.Threading.CancellationToken cancellationToken)";
            builder.Summary("Enforces the effective Screenplay authorization for one operation.")
                .OpenBlock($"public sealed class {Name(operation.Id)} : global::Cratis.Arc.Authorization.IAuthorizationPolicy")
                .Line("/// <inheritdoc/>");
            if (SemanticCratisAdmission.OpaquePolicies(operation.Authorization, context.Application.Policies).Any())
            {
                // Terms evaluate left to right with C# short-circuiting, as Screenplay's ordered gates do. A reached
                // opaque term that cannot be given its context denies the whole authorization rather than becoming false.
                builder.OpenBlock(signature)
                    .Line("var unavailable = false;")
                    .Line($"var allowed = {expression};")
                    .Line("return global::System.Threading.Tasks.ValueTask.FromResult(allowed && !unavailable);")
                    .EndBlock();
            }
            else
            {
                builder.ExpressionMember(signature, $"global::System.Threading.Tasks.ValueTask.FromResult({expression})");
            }

            // Arc strips guest roles and claims. Preserve this operation's exact anonymous opt-in.
            var anonymous = AllowsGuest(operation.Authorization, context, operation.Properties, operation.Subject) ? ", evaluatesAnonymous: true" : string.Empty;
            var registration = context.Domain.Count == 0 ? "Registration" : $"global::{context.RootNamespace}.GeneratedPolicies.Registration";
            var policyName = CSharpCodeBuilder.StringLiteral(PolicyName(context, operation.Id));
            builder.BlankLine()
                .Line("// Module initialization registers a known generated policy, without assembly scanning.")
                .Line("#pragma warning disable CA2255 // Generated application registration is intentionally initialized at module load.")
                .Line("[global::System.Runtime.CompilerServices.ModuleInitializer]")
                .OpenBlock("internal static void RegisterPolicy()")
                .Line($"{registration}.Add({policyName}, services => services.AddArcAuthorizationPolicy<{Name(operation.Id)}>({policyName}{anonymous}));")
                .EndBlock()
                .Line("#pragma warning restore CA2255")
                .EndBlock();
            yield return new(Path.Combine(context.PoliciesFolder, $"{Name(operation.Id)}.cs"), builder.ToString()) { Sources = [operation.Id] };
        }
    }

    /// <summary>
    /// Renders the selection-independent policy registry and value helpers.
    /// </summary>
    /// <param name="context">The semantic application.</param>
    /// <returns>The shared runtime file.</returns>
    public static RenderedFile RenderShared(SemanticApplicationContext context)
    {
        var builder = new CSharpCodeBuilder().Namespace($"{context.RootNamespace}.GeneratedPolicies");
        builder.Summary("Registers every generated authorization policy with Arc.")
            .OpenBlock("public static partial class Registration")
            .Line("static readonly global::System.Collections.Generic.List<(string Name, global::System.Action<global::Microsoft.Extensions.DependencyInjection.IServiceCollection> Apply)> _policies = [];")
            .BlankLine()
            .ExpressionMember("internal static void Add(string name, global::System.Action<global::Microsoft.Extensions.DependencyInjection.IServiceCollection> apply)", "_policies.Add((name, apply))")
            .BlankLine()
            .OpenBlock("static partial void RegisterGenerated(global::Microsoft.Extensions.DependencyInjection.IServiceCollection services)")
            .OpenBlock("foreach (var policy in _policies.OrderBy(policy => policy.Name, global::System.StringComparer.Ordinal))")
            .Line("policy.Apply(services);")
            .EndBlock().EndBlock().EndBlock().BlankLine();

        // Reflection is limited to the declared public property path, using ordinal names. Missing values
        // deny; an empty-string text target matches an empty claim. Uuid, Date and DateTime targets must be canonical.
        builder.OpenBlock("internal static class PolicyValues")
            .ExpressionMember(
                "public static bool Match(global::Cratis.Arc.Authorization.AuthorizationPolicyContext context, string claim, string? target)",
                "target is not null && context.Principal.Claims.Any(value => global::System.String.Equals(value.Type, claim, global::System.StringComparison.OrdinalIgnoreCase) && global::System.String.Equals(value.Value, target, global::System.StringComparison.Ordinal))");

        // Always emit Screenplay's three-valued helpers: shared bytes never depend on selected operations.
        // Missing targets stay unknown under negation; only a definite true allows.
        builder.ExpressionMember(
                "public static bool? Truth(global::Cratis.Arc.Authorization.AuthorizationPolicyContext context, string claim, string? target)",
                "target is null ? null : Match(context, claim, target)")
            .ExpressionMember("public static bool? Not(bool? value)", "value is null ? null : !value.Value")
            .ExpressionMember("public static bool? And(bool? left, bool? right)", "(left, right) switch { (false, _) or (_, false) => false, (true, true) => true, _ => null }")
            .ExpressionMember("public static bool? Or(bool? left, bool? right)", "(left, right) switch { (true, _) or (_, true) => true, (false, false) => false, _ => null }");

        builder
            .OpenBlock("public static string? Uuid(object? value)")
            .Line("if (value is global::System.Guid uuid) return uuid.ToString(\"D\");")
            .Line("if (value is string text && global::System.Guid.TryParseExact(text, \"D\", out var parsed) &&")
            .Line("    global::System.String.Equals(text, parsed.ToString(\"D\"), global::System.StringComparison.Ordinal)) return text;")
            .Line("return null;")
            .EndBlock()
            .OpenBlock("public static string? Date(object? value)")
            .Line("if (value is global::System.DateOnly date) return date.ToString(\"yyyy-MM-dd\", global::System.Globalization.CultureInfo.InvariantCulture);")
            .Line("if (value is string text && global::System.DateOnly.TryParseExact(text, \"yyyy-MM-dd\", global::System.Globalization.CultureInfo.InvariantCulture, global::System.Globalization.DateTimeStyles.None, out _)) return text;")
            .Line("return null;")
            .EndBlock()
            .OpenBlock("public static string? DateTime(object? value)")
            .Line("if (value is global::System.DateTimeOffset instant) return instant.Offset == global::System.TimeSpan.Zero ? instant.UtcDateTime.ToString(\"O\", global::System.Globalization.CultureInfo.InvariantCulture) : instant.ToString(\"O\", global::System.Globalization.CultureInfo.InvariantCulture);")
            .Line("if (value is string text && global::System.DateTimeOffset.TryParseExact(text, \"O\", global::System.Globalization.CultureInfo.InvariantCulture, global::System.Globalization.DateTimeStyles.RoundtripKind, out var parsed) &&")
            .Line("    global::System.String.Equals(text, DateTime(parsed), global::System.StringComparison.Ordinal)) return text;")
            .Line("return null;")
            .EndBlock()
            .OpenBlock("public static string? Value(object? value, bool uuidTarget = false, bool dateTarget = false, bool dateTimeTarget = false)")
            .Line("if (value is string text) return dateTarget ? Date(text) : dateTimeTarget ? DateTime(text) : uuidTarget ? Uuid(text) : text;")
            .Line("if (value is global::System.DateOnly) return dateTarget ? Date(value) : null;")
            .Line("if (value is global::System.DateTimeOffset) return dateTimeTarget ? DateTime(value) : null;")
            .Line("if (value is global::System.Guid uuid) return dateTarget || dateTimeTarget ? null : Uuid(uuid);")
            .Line("if (value is null) return null;")
            .Line("var type = value.GetType();")
            .Line("if (!InheritsSupportedConcept(type)) return null;")
            .Line("return Value(type.GetProperty(\"Value\", global::System.Reflection.BindingFlags.Instance | global::System.Reflection.BindingFlags.Public)?.GetValue(value), uuidTarget, dateTarget, dateTimeTarget);")
            .EndBlock()
            .OpenBlock("static bool InheritsSupportedConcept(global::System.Type type)")
            .Line("for (var current = type.BaseType; current is not null; current = current.BaseType)")
            .Line("{")
            .Line("    if (current.IsGenericType && current.GenericTypeArguments.Length == 1 && (current.GenericTypeArguments[0] == typeof(string) || current.GenericTypeArguments[0] == typeof(global::System.Guid) || current.GenericTypeArguments[0] == typeof(global::System.DateOnly) || current.GenericTypeArguments[0] == typeof(global::System.DateTimeOffset)) &&")
            .Line("        (current.GetGenericTypeDefinition().FullName == \"Cratis.Concepts.ConceptAs`1\" || current.GetGenericTypeDefinition().FullName == \"Cratis.Chronicle.Events.EventSourceId`1\")) return true;")
            .Line("}")
            .Line("return false;")
            .EndBlock()
            .OpenBlock("public static string? Path(object? value, string path, bool uuidTarget = false, bool dateTarget = false, bool dateTimeTarget = false)")
            .Line("foreach (var segment in path.Split('.'))")
            .Line("{")
            .Line("    if (value is null) return null;")
            .Line("    value = value.GetType().GetProperty(segment, global::System.Reflection.BindingFlags.Instance | global::System.Reflection.BindingFlags.Public)?.GetValue(value);")
            .Line("}")
            .Line("return Value(value, uuidTarget, dateTarget, dateTimeTarget);")
            .EndBlock()
            .OpenBlock("public static string? Query(global::Cratis.Arc.Authorization.AuthorizationPolicyContext context, string argument, string path, bool uuidTarget = false, bool dateTarget = false, bool dateTimeTarget = false)")
            .Line("if (context.Target is not global::System.Reflection.MethodInfo method || !method.GetParameters().Any(parameter => string.Equals(parameter.Name, argument, global::System.StringComparison.Ordinal)) ||")
            .Line("    context.Resource is not global::Cratis.Arc.Queries.QueryContext { Arguments: { } arguments } || !arguments.TryGetValue(argument, out var key)) return null;")
            .Line("return path == argument ? Value(key, uuidTarget, dateTarget, dateTimeTarget) : Path(key, path[(argument.Length + 1)..], uuidTarget, dateTarget, dateTimeTarget);")
            .EndBlock()
            .EndBlock();

        return new(Path.Combine("GeneratedPolicies", "Policies.cs"), builder.ToString()) { Sources = [context.Application.Id] };
    }

    /// <summary>
    /// Renders the verified opaque policy bodies and their typed context, when a rendered operation uses one.
    /// </summary>
    /// <param name="context">The semantic application.</param>
    /// <param name="slices">The rendered slices.</param>
    /// <returns>The rendered files; empty when no rendered operation uses an opaque policy.</returns>
    public static IEnumerable<RenderedFile> RenderOpaque(SemanticApplicationContext context, IReadOnlyList<LocatedSemanticSlice> slices)
    {
        var bodies = OpaqueSites(context, slices).Select(site =>
        {
            var diagnostics = System.Collections.Immutable.ImmutableArray.CreateBuilder<Contracts.Rendering.ArtifactRenderDiagnostic>();
            if (!SemanticImplementationAdmission.TryGetVerifiedBody(context.Request, site.RequirementId, site.Operation, diagnostics, out var body))
            {
                throw new InvalidTypedContext($"Policy requirement '{site.RequirementId}' lost its verified body after admission.");
            }

            return (Descriptor: Descriptor(context, site.RequirementId, site.Operation), Body: body!);
        }).OrderBy(_ => SemanticPolicyContextRuntime.Body(_.Descriptor), StringComparer.Ordinal).ToArray();
        if (bodies.Length == 0)
        {
            return [];
        }

        return new[]
        {
            SemanticPolicyContextRuntime.RenderRuntime(context) with { Sources = [context.Application.Id] },
            SemanticPolicyContextRuntime.RenderSharedBodies(context) with { Sources = [context.Application.Id] }
        }.Concat(bodies.SelectMany(site => new[]
        {
            SemanticPolicyContextRuntime.RenderWrapper(context, site.Descriptor) with { Sources = [site.Descriptor.OperationId!.Value] },
            SemanticPolicyContextRuntime.RenderBodies(context, [site], evaluators: true) with
            {
                RelativePath = Path.Combine(context.PoliciesFolder, $"PolicyBodies_{SemanticTypedContextRenderer.Suffix(site.Descriptor)}.cs"),
                Sources = [site.Descriptor.OperationId!.Value]
            }
        }));
    }

    internal static IEnumerable<(string RequirementId, SemanticId Operation)> OpaqueSites(SemanticApplicationContext context, IReadOnlyList<LocatedSemanticSlice> slices) =>
        slices.SelectMany(slice => slice.Slice.Commands.Select(command => (command.Id, command.Authorization))
                .Concat(slice.Slice.Queries.Select(query => (query.Id, query.Authorization))))
            .Where(operation => operation.Authorization is not null)
            .SelectMany(operation => SemanticCratisAdmission.OpaquePolicies(operation.Authorization!, context.Application.Policies)
                .Select(policy => (((SemanticOpaquePolicyCondition)policy.Condition).RequirementId, operation.Id)))
            .Distinct();

    static SemanticTypedContextDescriptor Descriptor(SemanticApplicationContext context, string requirementId, SemanticId operation) =>
        context.Request.TypedContextDescriptors.Single(descriptor => descriptor.RequirementId == requirementId && descriptor.OperationId == operation);

    static string Authorization(SemanticAuthorization authorization, SemanticApplicationContext context, IReadOnlyList<SemanticProperty> properties, bool command, string argument, string subject, SemanticId operation) => authorization switch
    {
        // Admission accepts an opaque predicate only as a whole named policy; its term is the verified body.
        SemanticPolicyReference reference when context.Application.Policies.Single(policy => policy.Name == reference.Name).Condition is SemanticOpaquePolicyCondition opaque =>
            Opaque(opaque, context, properties, command, argument, subject, operation),

        // A policy without negation keeps its two-valued rendering: without `not`, mapping unknown to false decides
        // exactly as three-valued logic followed by deny-on-unknown, so the generated bytes stay unchanged.
        SemanticPolicyReference reference => context.Application.Policies.Single(policy => policy.Name == reference.Name).Condition is var condition && Negates(condition)
            ? $"({Truth(condition, context, properties, command, argument, subject)} == true)"
            : Condition(context.Application.Policies.Single(policy => policy.Name == reference.Name).Condition, context, properties, command, argument, subject),
        SemanticLogicalAuthorization { Operator: SemanticLogicalOperator.And } logical => $"({Authorization(logical.Left, context, properties, command, argument, subject, operation)} && {Authorization(logical.Right, context, properties, command, argument, subject, operation)})",
        SemanticLogicalAuthorization { Operator: SemanticLogicalOperator.Or } logical => $"({Authorization(logical.Left, context, properties, command, argument, subject, operation)} || {Authorization(logical.Right, context, properties, command, argument, subject, operation)})",
        _ => throw UnsupportedSemanticRendering.For(nameof(SemanticAuthorization), authorization.GetType().Name)
    };

    static string Opaque(SemanticOpaquePolicyCondition opaque, SemanticApplicationContext context, IReadOnlyList<SemanticProperty> properties, bool command, string argument, string subject, SemanticId operation)
    {
        var descriptor = Descriptor(context, opaque.RequirementId, operation);
        if (!context.PolicyContextReads.TryGetValue((opaque.RequirementId, operation), out var reads))
        {
            throw new InvalidTypedContext($"Policy requirement '{opaque.RequirementId}' lost its analysed context reads after admission.");
        }

        // Screenplay supplies an empty Subject when the identifier is unavailable. A body that never reads Subject
        // is given the empty text rather than a value Stage would have to resolve.
        var source = descriptor.Members.Single(member => member.Name == "Subject").Source.Kind;
        string text;
        if (!reads.Contains("Subject") || source == SemanticContextSourceKinds.Unavailable)
        {
            text = "global::System.String.Empty";
        }
        else
        {
            var targetType = SemanticClaimTargets.Primitive(context, SemanticClaimTargets.Property(context, properties, subject)) switch
            {
                SemanticPrimitiveType.Uuid => ", true",
                SemanticPrimitiveType.Date => ", dateTarget: true",
                SemanticPrimitiveType.DateTime => ", dateTimeTarget: true",
                _ => string.Empty
            };
            text = command
                ? $"PolicyValues.Path((context.Resource as global::Cratis.Arc.Commands.CommandContext)?.Command, {Literal(PascalPath(subject))}{targetType})"
                : $"PolicyValues.Query(context, {Literal(argument)}, {Literal(argument)}{targetType})";
        }

        return $"PolicyBodies.{SemanticPolicyContextRuntime.Evaluate(descriptor)}(ref unavailable, context, {text})";
    }

    static string Condition(SemanticPolicyCondition condition, SemanticApplicationContext context, IReadOnlyList<SemanticProperty> properties, bool command, string argument, string subject) => condition switch
    {
        SemanticAuthenticatedCondition => "context.Principal.Identity?.IsAuthenticated == true",
        SemanticRoleCondition role => $"context.Principal.IsInRole({Literal(role.Role)})",
        SemanticClaimCondition claim => Claim(claim, context, properties, command, argument, subject),
        SemanticLogicalPolicyCondition { Operator: SemanticLogicalOperator.And } logical => $"({Condition(logical.Left, context, properties, command, argument, subject)} && {Condition(logical.Right, context, properties, command, argument, subject)})",
        SemanticLogicalPolicyCondition { Operator: SemanticLogicalOperator.Or } logical => $"({Condition(logical.Left, context, properties, command, argument, subject)} || {Condition(logical.Right, context, properties, command, argument, subject)})",
        _ => throw UnsupportedSemanticRendering.For(nameof(SemanticPolicyCondition), condition.GetType().Name)
    };

    static bool AllowsGuest(SemanticAuthorization authorization, SemanticApplicationContext context, IReadOnlyList<SemanticProperty> properties, string subject) => authorization switch
    {
        // An opaque body is unknown until it runs, so it never makes an operation guest-satisfiable.
        SemanticPolicyReference reference when context.Application.Policies.Single(policy => policy.Name == reference.Name).Condition is SemanticOpaquePolicyCondition => false,

        // Unknown denies at each named policy boundary, before composing the effective authorization.
        SemanticPolicyReference reference => GuestTruth(context.Application.Policies.Single(policy => policy.Name == reference.Name).Condition, context, properties, subject) == true,
        SemanticLogicalAuthorization { Operator: SemanticLogicalOperator.And } logical => AllowsGuest(logical.Left, context, properties, subject) && AllowsGuest(logical.Right, context, properties, subject),
        SemanticLogicalAuthorization { Operator: SemanticLogicalOperator.Or } logical => AllowsGuest(logical.Left, context, properties, subject) || AllowsGuest(logical.Right, context, properties, subject),
        _ => throw UnsupportedSemanticRendering.For(nameof(SemanticAuthorization), authorization.GetType().Name)
    };

    // A guest has no authentication, roles or claims. A supported claim target can be supplied by the request,
    // so its comparison is false for that guest. Missing/null targets remain unknown in the runtime policy;
    // a statically non-text target is always unknown and cannot justify anonymous opt-in even under `not`.
    static bool? GuestTruth(SemanticPolicyCondition condition, SemanticApplicationContext context, IReadOnlyList<SemanticProperty> properties, string subject) => condition switch
    {
        SemanticAuthenticatedCondition or SemanticRoleCondition => false,
        SemanticClaimCondition claim => claim.TargetKind == SemanticClaimTargetKind.Literal ||
            SemanticClaimTargets.Primitive(context, SemanticClaimTargets.Property(context, properties, claim.TargetKind == SemanticClaimTargetKind.Subject ? subject : claim.Value!)) is SemanticPrimitiveType.Text or SemanticPrimitiveType.Uuid or SemanticPrimitiveType.Date or SemanticPrimitiveType.DateTime ? false : null,
        SemanticNotPolicyCondition not => GuestTruth(not.Operand, context, properties, subject) is { } value ? !value : null,
        SemanticLogicalPolicyCondition { Operator: SemanticLogicalOperator.And } logical => And(GuestTruth(logical.Left, context, properties, subject), GuestTruth(logical.Right, context, properties, subject)),
        SemanticLogicalPolicyCondition { Operator: SemanticLogicalOperator.Or } logical => Or(GuestTruth(logical.Left, context, properties, subject), GuestTruth(logical.Right, context, properties, subject)),
        _ => throw UnsupportedSemanticRendering.For(nameof(SemanticPolicyCondition), condition.GetType().Name)
    };

    static bool? And(bool? left, bool? right) => (left, right) switch
    {
        (false, _) or (_, false) => false,
        (true, true) => true,
        _ => null
    };

    static bool? Or(bool? left, bool? right) => (left, right) switch
    {
        (true, _) or (_, true) => true,
        (false, false) => false,
        _ => null
    };

    static bool Negates(SemanticPolicyCondition condition) => condition switch
    {
        SemanticNotPolicyCondition => true,
        SemanticLogicalPolicyCondition logical => Negates(logical.Left) || Negates(logical.Right),
        _ => false
    };

    // Unknown is modeled explicitly as null and combined only through PolicyValues.Not/And/Or (Kleene logic),
    // never through Boolean negation or short-circuit operators.
    static string Truth(SemanticPolicyCondition condition, SemanticApplicationContext context, IReadOnlyList<SemanticProperty> properties, bool command, string argument, string subject) => condition switch
    {
        SemanticAuthenticatedCondition => "context.Principal.Identity?.IsAuthenticated == true",
        SemanticRoleCondition role => $"context.Principal.IsInRole({Literal(role.Role)})",
        SemanticClaimCondition claim => Claim(claim, context, properties, command, argument, subject, truth: true),
        SemanticNotPolicyCondition not => $"PolicyValues.Not({Truth(not.Operand, context, properties, command, argument, subject)})",
        SemanticLogicalPolicyCondition { Operator: SemanticLogicalOperator.And } logical => $"PolicyValues.And({Truth(logical.Left, context, properties, command, argument, subject)}, {Truth(logical.Right, context, properties, command, argument, subject)})",
        SemanticLogicalPolicyCondition { Operator: SemanticLogicalOperator.Or } logical => $"PolicyValues.Or({Truth(logical.Left, context, properties, command, argument, subject)}, {Truth(logical.Right, context, properties, command, argument, subject)})",
        _ => throw UnsupportedSemanticRendering.For(nameof(SemanticPolicyCondition), condition.GetType().Name)
    };

    static string Claim(SemanticClaimCondition claim, SemanticApplicationContext context, IReadOnlyList<SemanticProperty> properties, bool command, string argument, string subject, bool truth = false)
    {
        var primitive = claim.TargetKind == SemanticClaimTargetKind.Literal ? SemanticPrimitiveType.Text :
            SemanticClaimTargets.Primitive(context, SemanticClaimTargets.Property(context, properties, claim.TargetKind == SemanticClaimTargetKind.Subject ? subject : claim.Value!));

        // A number or Boolean is never a claim value. Two-valued rendering denies it outright; three-valued rendering
        // makes it unknown, so a negation over it can never allow, whatever a transport supplies at run time.
        if (primitive is SemanticPrimitiveType.WholeNumber or SemanticPrimitiveType.DecimalNumber or SemanticPrimitiveType.Boolean)
        {
            return truth ? "null" : "false";
        }

        var targetType = primitive switch
        {
            SemanticPrimitiveType.Uuid => ", true",
            SemanticPrimitiveType.Date => ", dateTarget: true",
            SemanticPrimitiveType.DateTime => ", dateTimeTarget: true",
            _ => string.Empty
        };
        var target = claim.TargetKind switch
        {
            SemanticClaimTargetKind.Literal => Literal(claim.Value!),
            SemanticClaimTargetKind.Artifact when command => $"PolicyValues.Path((context.Resource as global::Cratis.Arc.Commands.CommandContext)?.Command, {Literal(PascalPath(claim.Value!))}{targetType})",
            SemanticClaimTargetKind.Artifact => $"PolicyValues.Query(context, {Literal(argument)}, {Literal(QueryPath(claim.Value!))}{targetType})",
            SemanticClaimTargetKind.Subject when command => $"PolicyValues.Path((context.Resource as global::Cratis.Arc.Commands.CommandContext)?.Command, {Literal(PascalPath(subject))}{targetType})",
            SemanticClaimTargetKind.Subject => $"PolicyValues.Query(context, {Literal(argument)}, {Literal(argument)}{targetType})",
            _ => throw UnsupportedSemanticRendering.For(nameof(SemanticClaimTargetKind), claim.TargetKind)
        };
        return $"PolicyValues.{(truth ? "Truth" : "Match")}(context, {Literal(claim.Claim)}, {target})";
    }

    static string PascalPath(string path) => string.Join('.', path.Split('.').Select(Identifiers.ToPascalCase));

    static string QueryPath(string path) => path.Split('.') is { Length: > 1 } parts
        ? $"{Identifiers.ToCamelCase(parts[0])}.{PascalPath(string.Join('.', parts.Skip(1)))}"
        : Identifiers.ToCamelCase(path);

    static string Literal(string value) => JsonSerializer.Serialize(value);
}
