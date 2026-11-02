// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Rendering.Cratis.Semantics.Policies;
using Microsoft.CodeAnalysis;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>Recognizes the allocated typed-context declarations by exact full name.</summary>
internal static class TypedContextTypes
{
    /// <summary>The fixed runtime names in the typed-context and policy namespaces. They are not wrappers and are reserved by the allocator.</summary>
    internal static readonly string[] Fixed =
    [
        "TenantId",
        "ReducerContextValues",
        "Claim",
        SemanticPolicyContextRuntime.IdentityType,
        "PolicyContextValues",
        "PolicyArtifactUnavailable",
        "PolicyTenantUnavailable",
        "PolicyContext",
        "PolicyBodies",
        "ValidationBodies"
    ];

    /// <summary>
    /// Determines whether a type is exactly one of the allocated wrappers or local query shapes of the application.
    /// Membership is by full name; a peer in the same namespace, another application's namespace or a fixed runtime
    /// type is never a context.
    /// </summary>
    /// <param name="type">The type to test.</param>
    /// <param name="contextTypes">The full names of the allocated wrappers and local shapes.</param>
    /// <returns>True when the type is an allocated typed context or local shape.</returns>
    internal static bool IsContext(ITypeSymbol? type, IReadOnlySet<string> contextTypes) =>
        type is INamedTypeSymbol { IsAnonymousType: false, IsGenericType: false } named && contextTypes.Contains(named.ToDisplayString());

    /// <summary>Creates the exact set of allocated wrapper and local shape full names for an application.</summary>
    /// <param name="context">The semantic application.</param>
    /// <returns>The full names, without the global alias.</returns>
    internal static IReadOnlySet<string> Names(SemanticApplicationContext context)
    {
        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (var descriptor in (context.Request.TypedContextDescriptors.IsDefault ? [] : context.Request.TypedContextDescriptors).Where(descriptor => descriptor is not null))
        {
            names.Add($"{context.TypedContextsNamespace}.{context.ContextNames.ContextWrapper(descriptor)}");
            foreach (var member in descriptor.Members.IsDefault ? [] : descriptor.Members)
            {
                if (member.Type?.Shape is { } shape && context.Queries.ContainsKey(shape))
                {
                    names.Add($"{context.TypedContextsNamespace}.{context.ContextNames.ContextShape(descriptor, member.Name)}");
                }
            }
        }
        return names;
    }
}
