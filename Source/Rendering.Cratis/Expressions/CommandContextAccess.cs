// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Projections;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Naming;

namespace Cratis.Stage.Rendering.Cratis.Expressions;

/// <summary>
/// Renders the context expressions inside a rendered command handler, and records the collaborators the handler
/// has to take a parameter for to reach them.
/// </summary>
/// <remarks>
/// <para>
/// A rendered command handler is an Arc model-bound <c>Handle()</c>. Arc's own <c>CommandContext</c> carries the
/// correlation id, the command instance and the dependencies — and none of what the Screenplay language names:
/// no <c>Occurred</c>, no <c>Identity</c>, no <c>Tenant</c>, no <c>CausedBy</c>, no <c>Causation</c>. Screenplay
/// defines those on its own <c>Cratis.Screenplay.Contexts.CommandContext</c>, which is a different type that a
/// rendered application never receives. Rendering <c>$context.occurred</c> as <c>context.Occurred</c> therefore
/// produced an application that did not compile.
/// </para>
/// <para>
/// Every path is rendered instead against something the Cratis runtime really offers, asked for as a handler
/// parameter — which is how a hand-written slice reaches the same values. A path with no such equivalent is
/// reported and rendered as a missing value rather than as a member that does not exist.
/// </para>
/// </remarks>
/// <param name="subject">What is being rendered, for diagnostics (for example <c>Command 'RegisterInvoice'</c>).</param>
/// <param name="diagnostics">Collects anything that could not be rendered faithfully.</param>
public sealed class CommandContextAccess(string subject, ICollection<string> diagnostics) : IExpressionContext
{
    readonly List<HandlerCollaborator> _collaborators = [];
    readonly SortedSet<string> _namespaces = new(StringComparer.Ordinal);

    /// <summary>
    /// Gets the collaborators the rendered handler needs, in the order they were first asked for.
    /// </summary>
    public IReadOnlyList<HandlerCollaborator> Collaborators => _collaborators;

    /// <summary>
    /// Gets every namespace the rendered expressions need in scope.
    /// </summary>
    public IEnumerable<string> Namespaces => _namespaces;

    /// <summary>
    /// The C# type a context value is rendered as, by path — so a caller can tell whether what the document maps
    /// it onto can hold it. A path the language does not name has no type, and neither has one that renders to a
    /// missing value.
    /// </summary>
    /// <param name="path">The <c>$context</c> path, without the <c>$context.</c> prefix.</param>
    /// <returns>The C# type name, or <see langword="null"/> when the path resolves to nothing typed.</returns>
    public static string? ValueTypeOf(string path) => path.Split('.') switch
    {
        ["occurred"] => "DateTimeOffset",
        ["tenant"] => "string",
        ["causedBy", "subject" or "name" or "userName"] => "string",
        ["causation", "type"] => "string",
        ["identity", "id" or "name" or "userName"] => "string",
        ["identity", "isAuthenticated"] => "bool",
        ["identity", "claims", ..] => "string",
        _ => null,
    };

    /// <inheritdoc/>
    public string Render(ContextExpressionSyntax context) => Resolve(context.Path);

    /// <inheritdoc/>
    public string Render(EventContextExpressionSyntax eventContext) =>
        Unrenderable($"$eventContext.{eventContext.Path}", "a command handler runs before anything is appended, so there is no event context");

    /// <inheritdoc/>
    public string Render(CausedByExpressionSyntax causedBy) =>
        causedBy.Property is null ? Identity(null) : Resolve($"causedBy.{causedBy.Property}");

    /// <inheritdoc/>
    public string RenderEventSourceId() =>
        Unrenderable("$eventSourceId", "the event source id is resolved from the command's own identifier rather than read in the handler");

    static string Property(string[] segments) => string.Join('.', segments.Skip(1).Select(Identifiers.ToPascalCase));

    string Resolve(string path)
    {
        var segments = path.Split('.');
        return segments switch
        {
            ["occurred"] => "DateTimeOffset.UtcNow",
            ["tenant"] => $"{Use(HandlerCollaborator.Tenants)}.Current.Value",
            ["command", ..] when segments.Length > 1 => Property(segments),
            ["causedBy", var value] => CausedBy(value, path),
            ["causation", "type"] => $"{Use(HandlerCollaborator.Causations)}.GetCurrentChain()[^1].Type.Value",
            ["identity", ..] => Identity(segments, path),
            _ => Unrenderable($"$context.{path}", "the language names no such value"),
        };
    }

    string CausedBy(string value, string path) => value switch
    {
        "subject" => Identity("Subject"),
        "name" => Identity("Name"),
        "userName" => Identity("UserName"),
        _ => Unrenderable($"$context.{path}", "the language names no such value"),
    };

    string Identity(string[] segments, string path) => segments switch
    {
        ["identity", "id"] => Identity("Subject"),
        ["identity", "name"] => Identity("Name"),
        ["identity", "userName"] => Identity("UserName"),
        ["identity", "isAuthenticated"] => $"{Use(HandlerCollaborator.Principals)}.Current?.Identity?.IsAuthenticated == true",
        ["identity", "roles"] => Roles(),
        ["identity", "claims", .. var claim] when claim.Length > 0 => Claim(string.Join('.', claim)),
        _ => Unrenderable($"$context.{path}", "the language names no such value"),
    };

    string Identity(string? property) =>
        property is null ? $"{Use(HandlerCollaborator.Identities)}.GetCurrent()" : $"{Use(HandlerCollaborator.Identities)}.GetCurrent().{property}";

    string Claim(string name) =>
        $"{Use(HandlerCollaborator.Principals)}.Current?.FindFirst({CSharpCodeBuilder.StringLiteral(name)})?.Value ?? string.Empty";

    string Roles()
    {
        var principals = Use(HandlerCollaborator.Principals);
        _namespaces.Add("System.Security.Claims");
        return $"({principals}.Current?.FindAll(ClaimTypes.Role) ?? []).Select(claim => claim.Value)";
    }

    string Unrenderable(string expression, string reason)
    {
        diagnostics.Add($"{subject} reads '{expression}', which the rendered handler cannot reach — {reason}; rendered as a missing value.");
        return "default!";
    }

    string Use(HandlerCollaborator collaborator)
    {
        if (!_collaborators.Contains(collaborator))
        {
            _collaborators.Add(collaborator);

            if (collaborator.Namespace.Length > 0)
            {
                _namespaces.Add(collaborator.Namespace);
            }
        }

        return collaborator.ParameterName;
    }
}
