// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Rendering.Cratis.Naming;

namespace Cratis.Stage.Rendering.Cratis.Semantics.Policies;

/// <summary>
/// Names the generated authorization policies after the modeled policy they enforce.
/// </summary>
/// <remarks>
/// <para>
/// The names are a pure function of the whole application model, never of the selected scope, so a scoped
/// render and an application render agree on every name and on the bytes of every shared policy file.
/// </para>
/// <para>
/// Operations that bind a modeled policy name to the same effective authorization share one class named
/// NamePolicy whose policy name is Name. When one name is bound to different effective
/// authorizations (for example because the policy reads a different subject), every operation of that name is
/// qualified as NameForOperation, adding feature path segments only when that is still ambiguous.
/// No hashes are used. Trade-off: modeling an operation that binds a name differently renames the existing
/// users of that name the next time their scope is rendered; the attribute and the class move together.
/// </para>
/// </remarks>
internal static class SemanticPolicyNames
{
    static readonly ConditionalWeakTable<SemanticApplicationContext, IReadOnlyDictionary<SemanticId, PolicyName>> _cache = [];

    /// <summary>
    /// Gets the generated name of every protected operation of the application.
    /// </summary>
    /// <param name="context">The semantic application.</param>
    /// <returns>The policy names by operation identity.</returns>
    public static IReadOnlyDictionary<SemanticId, PolicyName> For(SemanticApplicationContext context) =>
        _cache.GetValue(context, Compute);

    /// <summary>
    /// Gets the policy name Arc knows an operation's policy by, including the domain placement.
    /// </summary>
    /// <param name="context">The semantic application.</param>
    /// <param name="operation">The protected operation.</param>
    /// <returns>The policy name used in the authorize attribute and the registration.</returns>
    public static string PolicyNameOf(SemanticApplicationContext context, SemanticId operation) =>
        string.Join('.', context.Domain.Append(For(context)[operation].Name));

    /// <summary>
    /// Gets the protected operations of the slices.
    /// </summary>
    /// <param name="slices">The slices.</param>
    /// <returns>The protected operations, in operation identity order.</returns>
    public static IReadOnlyList<PolicyOperation> Operations(IEnumerable<LocatedSemanticSlice> slices) =>
    [
        .. slices.SelectMany(located => located.Slice.Commands.Where(command => command.Authorization is not null)
                .Select(command => new PolicyOperation(command.Id, command.Name, command.Authorization!, located, command, null)))
            .Concat(slices.SelectMany(located => located.Slice.Queries.Where(query => query.Authorization is not null)
                .Select(query => new PolicyOperation(query.Id, query.Name, query.Authorization!, located, null, query))))
            .OrderBy(operation => operation.Id.ToString(), StringComparer.Ordinal)
    ];

    static IReadOnlyDictionary<SemanticId, PolicyName> Compute(SemanticApplicationContext context)
    {
        var operations = Operations(context.AllSlices);
        var bases = operations.ToDictionary(operation => operation.Id, operation => Base(operation.Authorization));
        var signatures = operations.ToDictionary(operation => operation.Id, operation => Signature(context, operation));
        var result = new Dictionary<SemanticId, PolicyName>();
        var qualified = new List<PolicyOperation>();
        var plain = new HashSet<string>(StringComparer.Ordinal);
        foreach (var group in operations.GroupBy(operation => bases[operation.Id], StringComparer.Ordinal).OrderBy(group => group.Key, StringComparer.Ordinal))
        {
            if (group.Select(operation => signatures[operation.Id]).Distinct(StringComparer.Ordinal).Count() == 1)
            {
                var sources = group.Select(operation => operation.Id).ToArray();
                plain.Add(group.Key);
                foreach (var operation in group)
                {
                    result[operation.Id] = new($"{group.Key}Policy", group.Key, sources);
                }
            }
            else
            {
                qualified.AddRange(group);
            }
        }

        var levels = qualified.ToDictionary(operation => operation.Id, _ => 0);
        var changed = true;
        while (changed)
        {
            changed = false;
            foreach (var collision in qualified.GroupBy(operation => Qualified(bases[operation.Id], operation, levels[operation.Id]), StringComparer.Ordinal)
                .Where(group => group.Count() > 1 || plain.Contains(group.Key)))
            {
                foreach (var operation in collision.Where(operation => levels[operation.Id] <= operation.Located.FeaturePath.Count))
                {
                    levels[operation.Id]++;
                    changed = true;
                }
            }
        }

        foreach (var operation in qualified)
        {
            var name = Qualified(bases[operation.Id], operation, levels[operation.Id]);
            result[operation.Id] = new($"{name}Policy", name, [operation.Id]);
        }

        return result;
    }

    // Level 0 is the operation name; each further level adds the next outer feature, and the last adds the module.
    static string Qualified(string name, PolicyOperation operation, int level)
    {
        var segments = Segments(operation, level);

        return $"{name}For{string.Concat(segments.Select(GeneratedPascalCase.From))}{GeneratedPascalCase.From(operation.Name)}";
    }

    static IEnumerable<string> Segments(PolicyOperation operation, int level)
    {
        var features = operation.Located.FeaturePath;
        if (level == 0)
        {
            return [];
        }

        return level <= features.Count
            ? features.Skip(features.Count - level).Select(feature => feature.Name)
            : [operation.Located.Module.Name, .. features.Select(feature => feature.Name)];
    }

    static string Base(SemanticAuthorization authorization) => authorization switch
    {
        SemanticPolicyReference reference => GeneratedPascalCase.From(reference.Name),
        SemanticLogicalAuthorization { Operator: SemanticLogicalOperator.And } logical => $"{Base(logical.Left)}And{Base(logical.Right)}",
        SemanticLogicalAuthorization { Operator: SemanticLogicalOperator.Or } logical => $"{Base(logical.Left)}Or{Base(logical.Right)}",
        _ => "Authorization"
    };

    // The effective signature is the rendered authorization and anonymous opt-in, without the class name. An opaque
    // term is a per-site body, so it never matches another operation. Naming must be total: an operation that
    // admission refuses is given a signature of its own and is reported by admission, not here.
    static string Signature(SemanticApplicationContext context, PolicyOperation operation)
    {
        try
        {
            var (properties, isCommand, argument, subject) = operation.Inputs();
            var expression = SemanticPolicyArtifactRenderer.Authorization(operation.Authorization, context, properties, isCommand, argument, subject, operation.Id, naming: true);
            var guest = SemanticPolicyArtifactRenderer.AllowsGuest(operation.Authorization, context, properties, subject);
            return $"{expression}|{guest}";
        }
        catch (Exception exception) when (exception is InvalidOperationException or KeyNotFoundException or ArgumentException or UnsupportedSemanticRendering or InvalidTypedContext)
        {
            return $"unrenderable:{operation.Id}";
        }
    }
}

/// <summary>
/// The generated name of the policy that guards one operation.
/// </summary>
/// <param name="ClassName">The policy class name.</param>
/// <param name="Name">The modeled policy name, before the domain placement.</param>
/// <param name="Sources">The operations that share the class, in ordinal order.</param>
internal sealed record PolicyName(string ClassName, string Name, IReadOnlyList<SemanticId> Sources);

/// <summary>
/// A protected command or query and what its policy is rendered from.
/// </summary>
/// <param name="Id">The operation identity.</param>
/// <param name="Name">The modeled operation name.</param>
/// <param name="Authorization">The effective authorization.</param>
/// <param name="Located">The slice that declares the operation.</param>
/// <param name="Command">The command, when the operation is one.</param>
/// <param name="Query">The query, when the operation is one.</param>
internal sealed record PolicyOperation(
    SemanticId Id,
    string Name,
    SemanticAuthorization Authorization,
    LocatedSemanticSlice Located,
    SemanticCommand? Command,
    SemanticKeyedQuery? Query)
{
    /// <summary>
    /// Gets what the authorization expression reads from the operation.
    /// </summary>
    /// <returns>The properties, whether it is a command, the query argument variable and the subject name.</returns>
    public (IReadOnlyList<SemanticProperty> Properties, bool IsCommand, string Argument, string Subject) Inputs()
    {
        if (Command is not null)
        {
            return (Command.Properties, true, string.Empty, Command.Properties.Single(property => property.IsIdentifier).Name);
        }

        var argument = Query!.Argument;
        return (argument is null ? [] : [new(argument.Id, argument.Name, argument.Type, false)],
            false,
            argument is null ? string.Empty : Identifiers.ToCamelCase(argument.Name),
            argument?.Name ?? string.Empty);
    }
}
