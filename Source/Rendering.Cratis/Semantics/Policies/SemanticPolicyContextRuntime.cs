// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;

namespace Cratis.Stage.Rendering.Cratis.Semantics.Policies;

/// <summary>
/// Emits the typed <c language="csharp">PolicyContext</c> realization for opaque policy bodies, without referencing the Screenplay
/// compiler at run time. Admission analyses bodies against these same declarations.
/// </summary>
internal static class SemanticPolicyContextRuntime
{
    /// <summary>The generated identity type, mirroring Screenplay's <c language="csharp">Identity</c>.</summary>
    internal const string IdentityType = "Identity";

    /// <summary>The method prefix of a generated policy body.</summary>
    internal const string BodyPrefix = "Body_";

    /// <summary>The members Stage supplies to an opaque policy body, in Screenplay's <c language="csharp">PolicyContext</c> order.</summary>
    internal static readonly string[] SuppliedMembers = ["Subject", "Identity", "Occurred"];

    /// <summary>
    /// The <c language="csharp">Identity</c> members with no defined mapping from Arc's <c language="csharp">ClaimsPrincipal</c>. Admission
    /// declares them so a body reading one is refused by name rather than by a compile error.
    /// </summary>
    internal static readonly string[] UnmappedIdentityMembers = ["Id", "Name", "UserName"];

    /// <summary>The <c language="csharp">PolicyContext</c> members Stage refuses to supply.</summary>
    internal static readonly string[] RefusedMembers = ["Artifact", "Tenant"];

    internal static string Wrapper(SemanticTypedContextDescriptor descriptor) => $"TypedContext_{SemanticTypedContextRenderer.Suffix(descriptor)}";

    internal static string Body(SemanticTypedContextDescriptor descriptor) => $"{BodyPrefix}{SemanticTypedContextRenderer.Suffix(descriptor)}";

    internal static string Evaluate(SemanticTypedContextDescriptor descriptor) => $"Evaluate_{SemanticTypedContextRenderer.Suffix(descriptor)}";

    internal static IEnumerable<(string Namespace, string Name)> GeneratedTypes(SemanticApplicationContext context, IReadOnlyList<LocatedSemanticSlice> slices)
    {
        var sites = SemanticPolicyArtifactRenderer.OpaqueSites(context, slices).ToHashSet();
        if (sites.Count == 0) yield break;

        yield return ("GeneratedPolicies", "PolicyBodies");
        yield return ("TypedContexts", "Claim");
        yield return ("TypedContexts", IdentityType);
        yield return ("TypedContexts", "PolicyContextValues");

        // Missing descriptors are refused separately; inventory only wrappers belonging to rendered use sites.
        foreach (var descriptor in context.Request.TypedContextDescriptors.IsDefault ? [] : context.Request.TypedContextDescriptors)
        {
            if (descriptor?.OperationId is { } operation && sites.Contains((descriptor.RequirementId, operation)))
            {
                yield return ("TypedContexts", Wrapper(descriptor));
            }
        }
    }

    /// <summary>
    /// Renders the identity types, the claims-principal mapping and one wrapper per used descriptor.
    /// </summary>
    /// <param name="context">The semantic application.</param>
    /// <param name="descriptors">The descriptors whose bodies are rendered.</param>
    /// <returns>The rendered runtime file.</returns>
    internal static RenderedFile RenderRuntime(SemanticApplicationContext context, IEnumerable<SemanticTypedContextDescriptor> descriptors)
    {
        var builder = new CSharpCodeBuilder().Namespace($"{context.RootNamespace}.TypedContexts");
        Identity(builder, analysis: false);
        foreach (var descriptor in descriptors)
        {
            builder.Summary("The policy context a verified opaque policy body reads.")
                .Line($"public sealed record {Wrapper(descriptor)}(string Subject, {IdentityType} Identity, global::System.DateTimeOffset Occurred);")
                .BlankLine();
        }

        // ClaimsIdentity.IsInRole compares the identity's role claim type ignoring case and the value ordinally,
        // which is the same lookup the portable `require role` condition renders as. Screenplay keeps roles and claims
        // separate, so role-type claims appear only in Roles, never in Claims or the claim lookups.
        builder.Summary("Maps Arc's authorization principal to the policy identity.")
            .OpenBlock("internal static class PolicyContextValues")
            .OpenBlock($"public static {IdentityType} From(global::System.Security.Claims.ClaimsPrincipal principal)")
            .Line("var roles = principal.Identities.SelectMany(identity => identity.Claims.Where(claim =>")
            .Line("    global::System.String.Equals(claim.Type, identity.RoleClaimType, global::System.StringComparison.OrdinalIgnoreCase))).Select(claim => claim.Value);")
            .Line("var claims = principal.Identities.SelectMany(identity => identity.Claims.Where(claim =>")
            .Line("    !global::System.String.Equals(claim.Type, identity.RoleClaimType, global::System.StringComparison.OrdinalIgnoreCase))).Select(claim => new Claim(claim.Type, claim.Value));")
            .Line($"return new {IdentityType}(principal.Identity?.IsAuthenticated == true, global::System.Collections.Immutable.ImmutableArray.CreateRange(roles), global::System.Collections.Immutable.ImmutableArray.CreateRange(claims));")
            .EndBlock()
            .EndBlock();
        return new(Path.Combine("TypedContexts", "PolicyContext.cs"), builder.ToString());
    }

    /// <summary>
    /// Renders the analysis-only declarations: the identity types with the unmapped members, the refused
    /// members' placeholder types and a five-member wrapper, so refused reads bind and can be named.
    /// </summary>
    /// <param name="context">The semantic application.</param>
    /// <param name="descriptor">The analysed descriptor.</param>
    /// <returns>The declarations, as a block-scoped namespace.</returns>
    internal static string AnalysisDeclarations(SemanticApplicationContext context, SemanticTypedContextDescriptor descriptor)
    {
        var builder = new CSharpCodeBuilder();
        Identity(builder, analysis: true);
        builder.Line("public sealed record PolicyArtifactUnavailable;")
            .Line("public sealed record PolicyTenantUnavailable;")
            .Line($"public sealed record {Wrapper(descriptor)}(PolicyArtifactUnavailable Artifact, string Subject, {IdentityType} Identity, PolicyTenantUnavailable Tenant, global::System.DateTimeOffset Occurred);");
        return $"namespace {context.RootNamespace}.TypedContexts\n{{\n{builder}\n}}\n";
    }

    /// <summary>
    /// Renders the class holding the verified bodies, optionally with their evaluation entry points.
    /// </summary>
    /// <param name="context">The semantic application.</param>
    /// <param name="bodies">The descriptor and verified body of each opaque policy use site.</param>
    /// <param name="evaluators">Whether to emit the entry points the generated policies call.</param>
    /// <returns>The rendered file.</returns>
    internal static RenderedFile RenderBodies(SemanticApplicationContext context, IEnumerable<(SemanticTypedContextDescriptor Descriptor, string Body)> bodies, bool evaluators)
    {
        // The body is compiled in this exact namespace, class and signature both by admission and by the
        // application; the class has no usings so only SDK implicit globals and enclosing namespaces bind.
        var contexts = $"global::{context.RootNamespace}.TypedContexts";
        var builder = new CSharpCodeBuilder().Namespace($"{context.RootNamespace}.GeneratedPolicies")
            .Summary("Evaluates verified opaque policy bodies against Stage's typed policy context.")
            .OpenBlock("internal static class PolicyBodies");
        foreach (var (descriptor, body) in bodies)
        {
            if (evaluators)
            {
                // A missing receipt time or an unresolvable subject makes the reached opaque term unavailable,
                // and the generated policy then denies the whole authorization: it is never treated as false.
                builder.OpenBlock($"public static bool {Evaluate(descriptor)}(ref bool unavailable, global::Cratis.Arc.Authorization.AuthorizationPolicyContext context, string? subject)")
                    .OpenBlock("if (context.ReceivedAt == default || subject is null)")
                    .Line("unavailable = true;")
                    .Line("return false;")
                    .EndBlock()
                    .BlankLine()
                    .Line($"return {Body(descriptor)}(new {contexts}.{Wrapper(descriptor)}(subject, {contexts}.PolicyContextValues.From(context.Principal), context.ReceivedAt));")
                    .EndBlock()
                    .BlankLine();
            }

            builder.OpenBlock($"static bool {Body(descriptor)}({contexts}.{Wrapper(descriptor)} context)")
                .RawVerbatim(body)
                .EndBlock()
                .BlankLine();
        }

        builder.EndBlock();
        return new(Path.Combine("GeneratedPolicies", "PolicyBodies.cs"), builder.ToString());
    }

    static void Identity(CSharpCodeBuilder builder, bool analysis)
    {
        var unmapped = analysis ? string.Concat(UnmappedIdentityMembers.Select(member => $"string {member}, ")) : string.Empty;
        builder.Summary("A claim the caller carries.")
            .Line("public sealed record Claim(string Name, string Value);")
            .BlankLine()
            .Summary("The caller a policy decides about.")
            .OpenBlock($"public sealed record {IdentityType}({unmapped}bool IsAuthenticated, global::System.Collections.Immutable.ImmutableArray<string> Roles, global::System.Collections.Immutable.ImmutableArray<Claim> Claims)")
            .ExpressionMember("public bool HasRole(string role)", "Roles.Contains(role, global::System.StringComparer.Ordinal)")
            .ExpressionMember("public bool HasClaim(string name)", "Claims.Any(claim => global::System.String.Equals(claim.Name, name, global::System.StringComparison.OrdinalIgnoreCase))")
            .ExpressionMember("public string? ClaimValue(string name)", "Claims.Where(claim => global::System.String.Equals(claim.Name, name, global::System.StringComparison.OrdinalIgnoreCase)).Select(claim => claim.Value).FirstOrDefault()")
            .ExpressionMember("public global::System.Collections.Immutable.ImmutableArray<string> ClaimValues(string name)", "global::System.Collections.Immutable.ImmutableArray.CreateRange(Claims.Where(claim => global::System.String.Equals(claim.Name, name, global::System.StringComparison.OrdinalIgnoreCase)).Select(claim => claim.Value))")
            .EndBlock()
            .BlankLine();
    }
}
