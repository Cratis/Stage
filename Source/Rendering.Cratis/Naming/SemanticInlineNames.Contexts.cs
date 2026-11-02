// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Rendering.Cratis.Semantics;

namespace Cratis.Stage.Rendering.Cratis.Naming;

internal sealed partial class SemanticInlineNames
{
    const string ContextScope = "typed-contexts";
    const string ContextFileScope = "typed-context-files";

    /// <summary>
    /// Creates the typed-context names. They must not read the generated type inventory, because the wrappers
    /// are part of that inventory; the model's own declarations are reserved directly instead.
    /// </summary>
    /// <param name="context">The semantic application.</param>
    /// <returns>The allocated names, one site per descriptor in the request, independent of render scope.</returns>
    internal static SemanticInlineNames CreateContexts(SemanticApplicationContext context)
    {
        var descriptors = (context.Request.TypedContextDescriptors.IsDefault ? [] : context.Request.TypedContextDescriptors)
            .Where(descriptor => descriptor is not null)
            .DistinctBy(ContextKey, StringComparer.Ordinal)
            .OrderBy(ContextKey, StringComparer.Ordinal);
        var sites = descriptors.Select(descriptor =>
        {
            var (stem, qualified) = ContextStems(context, descriptor);
            return new SemanticInlineNameSite(ContextKey(descriptor), stem, descriptor.RequirementId, ContextScope, null, ContextFileScope, qualified, name => ContextSpellings(name, descriptor));
        }).ToArray();
        var modeled = context.Concepts.Values.Select(concept => concept.Name)
            .Concat(context.Types.Values.Select(type => type.Name))
            .Concat(context.Events.Values.Select(@event => @event.Name))
            .Concat(context.Commands.Values.Select(command => command.Name))
            .Concat(context.ReadModels.Values.Select(model => model.Name))
            .Concat(context.Queries.Values.Select(query => query.Name))
            .Select(Identifiers.ToPascalCase)
            .Concat(SemanticCratisAdmission.NamingReservations(context, context.AllSlices).Select(type => type.Name))
            .Concat(TypedContextTypes.Fixed)
            .Distinct(StringComparer.Ordinal);
        return new(sites, modeled.Select(name => new SemanticInlineNameReservation(ContextScope, name)));
    }

    /// <summary>Gets the model-derived name every declaration of a typed context is built from.</summary>
    /// <param name="descriptor">The typed context.</param>
    /// <returns>The allocated base spelling.</returns>
    internal string ContextBase(SemanticTypedContextDescriptor descriptor) =>
        _names.TryGetValue(ContextKey(descriptor), out var name) ? name : $"{descriptor.Role}_{ContextDigest(descriptor)}";

    /// <summary>Gets the wrapper record name.</summary>
    /// <param name="descriptor">The typed context.</param>
    /// <returns>The wrapper name.</returns>
    internal string ContextWrapper(SemanticTypedContextDescriptor descriptor) => $"{ContextBase(descriptor)}Context";

    /// <summary>Gets the name of a local query shape declared next to a wrapper.</summary>
    /// <param name="descriptor">The typed context.</param>
    /// <param name="memberName">The wrapper member exposing the shape.</param>
    /// <returns>The shape name.</returns>
    internal string ContextShape(SemanticTypedContextDescriptor descriptor, string memberName) =>
        $"{ContextWrapper(descriptor)}{Identifiers.ToPascalCase(memberName)}Shape";

    /// <summary>Gets the validation or policy body method name.</summary>
    /// <param name="descriptor">The typed context.</param>
    /// <returns>The method name.</returns>
    internal string ContextMethod(SemanticTypedContextDescriptor descriptor) => ContextBase(descriptor);

    /// <summary>Gets the iterator core method name.</summary>
    /// <param name="descriptor">The typed context.</param>
    /// <returns>The core method name.</returns>
    internal string ContextCore(SemanticTypedContextDescriptor descriptor) => $"{ContextBase(descriptor)}Core";

    /// <summary>Gets the policy evaluator method name.</summary>
    /// <param name="descriptor">The typed context.</param>
    /// <returns>The evaluator method name.</returns>
    internal string ContextEvaluate(SemanticTypedContextDescriptor descriptor) => $"Evaluate{ContextBase(descriptor)}";

    /// <summary>Gets the file name of a policy body unit.</summary>
    /// <param name="descriptor">The typed context.</param>
    /// <returns>The file name.</returns>
    internal string ContextPolicyFile(SemanticTypedContextDescriptor descriptor) => $"PolicyBodies.{ContextBase(descriptor)}.cs";

    static string ContextKey(SemanticTypedContextDescriptor descriptor) => $"typed-context:{descriptor.RequirementId}:{descriptor.OperationId}";

    static string ContextDigest(SemanticTypedContextDescriptor descriptor) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{descriptor.RequirementId}:{descriptor.OperationId}")))[..8];

    static IEnumerable<string> ContextSpellings(string name, SemanticTypedContextDescriptor descriptor)
    {
        yield return $"{name}Context";
        yield return $"{name}Core";
        yield return $"Evaluate{name}";
        foreach (var member in descriptor.Members.IsDefault ? [] : descriptor.Members)
        {
            yield return $"{name}Context{Identifiers.ToPascalCase(member.Name)}Shape";
        }
    }

    static (string Stem, string? Qualified) ContextStems(SemanticApplicationContext context, SemanticTypedContextDescriptor descriptor)
    {
        var owner = descriptor.OperationId is { } operation ? ContextOwner(context, operation) : null;
        var stems = descriptor.Role switch
        {
            SemanticImplementationRole.ReducerTransition => TransitionStems(context, descriptor),
            SemanticImplementationRole.PolicyPredicate => PolicyStems(context, descriptor, owner),
            SemanticImplementationRole.RulePredicate or SemanticImplementationRole.CommandValidation or SemanticImplementationRole.ConceptValidation => RuleStems(context, descriptor, owner),
            _ => null
        };
        return stems ?? ($"{owner}{descriptor.Role}", null);
    }

    static (string Stem, string? Qualified)? TransitionStems(SemanticApplicationContext context, SemanticTypedContextDescriptor descriptor)
    {
        var reducer = context.Reducers.FirstOrDefault(candidate => candidate.ReadModel == descriptor.OperationId &&
            candidate.Transitions.Any(transition => transition.RequirementId == descriptor.RequirementId));
        var transition = reducer?.Transitions.First(candidate => candidate.RequirementId == descriptor.RequirementId);
        if (reducer is null || transition is null) return null;
        if (!context.Events.TryGetValue(transition.EventContract, out var @event)) return null;
        var eventName = Identifiers.ToPascalCase(@event.Name);
        return ($"{eventName}Transition", $"{Identifiers.ToPascalCase(reducer.Name)}{eventName}Transition");
    }

    static (string Stem, string? Qualified)? PolicyStems(SemanticApplicationContext context, SemanticTypedContextDescriptor descriptor, string? owner)
    {
        var policy = context.Application.Policies.FirstOrDefault(candidate =>
            candidate.Condition is SemanticOpaquePolicyCondition opaque && opaque.RequirementId == descriptor.RequirementId);
        if (policy is null) return null;
        var policyName = Identifiers.ToPascalCase(policy.Name);
        return (policyName, owner is null ? null : $"{policyName}For{owner}");
    }

    static (string Stem, string? Qualified) RuleStems(SemanticApplicationContext context, SemanticTypedContextDescriptor descriptor, string? owner)
    {
        var ownerName = owner ?? "Value";
        var rulePart = ContextRule(context, descriptor)?.Name is { Length: > 0 } rule ? Identifiers.ToPascalCase(rule) : null;
        if (rulePart is not null && Microsoft.CodeAnalysis.CSharp.SyntaxFacts.IsValidIdentifier(rulePart))
        {
            return (rulePart, $"{ownerName}{rulePart}");
        }

        var iterator = descriptor.Role != SemanticImplementationRole.RulePredicate;
        return (iterator ? $"Validate{ownerName}" : $"Rule{ownerName}", null);
    }

    static SemanticValidationRule? ContextRule(SemanticApplicationContext context, SemanticTypedContextDescriptor descriptor)
    {
        if (descriptor.OperationId is not { } operation) return null;
        if (context.Commands.TryGetValue(operation, out var command))
        {
            return command.Validations.IsDefaultOrEmpty ? null : command.Validations.FirstOrDefault(rule => rule.RequirementId == descriptor.RequirementId);
        }

        if (context.Concepts.TryGetValue(operation, out var concept))
        {
            return concept.Validations.IsDefaultOrEmpty ? null : concept.Validations.FirstOrDefault(rule => rule.RequirementId == descriptor.RequirementId);
        }

        return null;
    }

    static string? ContextOwner(SemanticApplicationContext context, SemanticId id)
    {
        if (context.Commands.TryGetValue(id, out var command)) return Identifiers.ToPascalCase(command.Name);
        if (context.Concepts.TryGetValue(id, out var concept)) return Identifiers.ToPascalCase(concept.Name);
        if (context.Queries.TryGetValue(id, out var query)) return Identifiers.ToPascalCase(query.Name);
        if (context.ReadModels.TryGetValue(id, out var model)) return Identifiers.ToPascalCase(model.Name);
        return null;
    }
}
