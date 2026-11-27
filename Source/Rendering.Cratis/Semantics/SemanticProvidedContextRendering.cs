// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Rendering.Cratis.CodeGeneration;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>Builds the portable context from the invocation's existing chained resolver.</summary>
internal static class SemanticProvidedContextRendering
{
    internal static void Render(CSharpCodeBuilder builder, SemanticApplicationContext context)
    {
        var translation = TenantTranslationSource.Render(context.RootNamespace).Content;

        // Reuse the shared, fully qualified translator declarations inside this generated resolver.
        builder.RawVerbatim(translation[translation.IndexOf("internal static class PortableTenantValues", StringComparison.Ordinal)..])
            .RawVerbatim("""
        static class StageProvideContext
        {
            public static async global::System.Threading.Tasks.ValueTask<global::Cratis.Screenplay.Contexts.ProvideContext> Create(
                global::Cratis.Choreography.Availability.Context.IContextValueResolver resolver,
                global::Cratis.Arc.Authorization.ICurrentPrincipalAccessor? principalAccessor,
                global::System.Threading.CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var tenant = await resolver.Resolve("$context.tenant", cancellationToken);
                var receipt = await resolver.Resolve("$context.receivedAt", cancellationToken);
                var user = await resolver.Resolve("$context.identity.user", cancellationToken);
                var system = await resolver.Resolve("$context.identity.isSystem", cancellationToken);
                var principal = principalAccessor?.Current ?? new global::System.Security.Claims.ClaimsPrincipal();
                var arguments = await resolver.Resolve("$context.arguments", cancellationToken);
                var payload = arguments.Value is global::System.Collections.Generic.IDictionary<string, object?> or global::System.Collections.Generic.IReadOnlyDictionary<string, object?> || arguments.Value is null
                    ? new ResolverValues(resolver, "$context.arguments.", cancellationToken)
                    : arguments.Value;
                var claims = global::System.Linq.Enumerable.ToArray(global::System.Linq.Enumerable.Select(
                    principal.Claims, claim => new global::Cratis.Screenplay.Contexts.Claim(claim.Type, claim.Value)));
                var roles = global::System.Linq.Enumerable.ToArray(global::System.Linq.Enumerable.Distinct(
                    global::System.Linq.Enumerable.Select(
                        global::System.Linq.Enumerable.Where(principal.Claims, claim => claim.Type == global::System.Security.Claims.ClaimTypes.Role || claim.Type is "role" or "roles"),
                        claim => claim.Value)));
                var portableTenant = tenant.Value is string value
                    ? PortableTenantValues.Translate(value,
                        global::Cratis.Arc.Tenancy.TenantId.Default.Value, global::Cratis.Arc.Tenancy.TenantId.NotSet.Value)
                    : global::Cratis.Screenplay.Contexts.TenantId.NotSet.Value;
                var id = user.Value as string ?? string.Empty;
                var identity = new global::Cratis.Screenplay.Contexts.Identity(
                    id, principal.Identity?.Name ?? id,
                    principal.FindFirst(global::System.Security.Claims.ClaimTypes.Name)?.Value ?? id,
                    principal.Identity?.IsAuthenticated ?? (id.Length > 0 && system.Value is not true), roles, claims);
                return new global::Cratis.Screenplay.Contexts.ProvideContext(
                    payload,
                    new global::Cratis.Screenplay.Contexts.TenantId(portableTenant),
                    identity,
                    receipt.Value is global::System.DateTimeOffset receivedAt ? receivedAt : default)
                {
                    Values = new ResolverValues(resolver, "$context.", cancellationToken)
                };
            }
        }

        sealed class ResolverValues(global::Cratis.Choreography.Availability.Context.IContextValueResolver resolver,
            string prefix, global::System.Threading.CancellationToken cancellationToken) : global::System.Dynamic.DynamicObject
        {
            public override bool TryGetMember(global::System.Dynamic.GetMemberBinder binder, out object? result)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var name = binder.Name;
                var camel = char.ToLowerInvariant(name[0]) + name[1..];
                var outcome = resolver.Resolve(prefix + camel, cancellationToken).AsTask().GetAwaiter().GetResult();
                if (!outcome.IsPresent && camel != name)
                {
                    outcome = resolver.Resolve(prefix + name, cancellationToken).AsTask().GetAwaiter().GetResult();
                }
                result = outcome.Value;
                return true;
            }
        }
        """);
    }
}
