// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;
using Cratis.Stage.Api;
using Microsoft.AspNetCore.Routing;

namespace Cratis.Stage.Host;

/// <summary>
/// Caches modeled persona principals by admitted operation, independently of request data.
/// </summary>
internal sealed class SandboxPersonas
{
    readonly ClaimsPrincipal? _fallback;
    readonly Dictionary<string, ClaimsPrincipal?> _operations = new(StringComparer.OrdinalIgnoreCase);

    internal SandboxPersonas(ApplicationSyntax application, ExecutableSemanticModel model, StageHttpSurface surface, ILogger logger)
    {
        _fallback = SandboxPersona.Create(application, logger);
        var defaultPersona = SandboxPersonaSelection.Select(application)?.Persona;
        var menuPersona = SandboxPersonaSelection.MenuPersona(application)?.Persona;
        var personas = application.Personas?.ToArray() ?? [];
        var principals = personas.ToDictionary(
            persona => persona.Name,
            persona => persona == defaultPersona ? _fallback : SandboxPersona.Create(persona, persona.Name, application, logger),
            StringComparer.Ordinal);
        var requirements = Requirements(model).ToDictionary(entry => entry.Artifact, entry => entry.Requirement, StringComparer.Ordinal);
        foreach (var operation in surface.Operations)
        {
            var requirement = requirements.GetValueOrDefault(operation.QualifiedArtifact);
            var persona = requirement is null ? null : Select(personas, menuPersona, requirement);
            var principal = persona is null ? _fallback : principals[persona.Name];
            _operations[Key(operation.Method, operation.CanonicalPath)] = principal;
            if (surface.AliasFor(operation.Method, operation.CanonicalPath) is { } alias)
            {
                _operations[Key(operation.Method, alias)] = principal;
            }
        }
    }

    internal ClaimsPrincipal? For(HttpContext context) =>
        context.GetEndpoint() is RouteEndpoint { RoutePattern.RawText: { } route }
            ? _operations.GetValueOrDefault(Key(context.Request.Method, route), _fallback)
            : _fallback;

    static PersonaSyntax? Select(PersonaSyntax[] personas, PersonaSyntax? menu, SemanticAuthorization requirement) =>
        menu is not null && Satisfies(requirement, menu.Policies.ToHashSet(StringComparer.Ordinal))
            ? menu
            : personas.FirstOrDefault(persona => Satisfies(requirement, persona.Policies.ToHashSet(StringComparer.Ordinal)));

    static bool Satisfies(SemanticAuthorization requirement, HashSet<string> policies) => requirement switch
    {
        SemanticPolicyReference reference => policies.Contains(reference.Name),
        SemanticLogicalAuthorization { Operator: SemanticLogicalOperator.And } logical => Satisfies(logical.Left, policies) && Satisfies(logical.Right, policies),
        SemanticLogicalAuthorization { Operator: SemanticLogicalOperator.Or } logical => Satisfies(logical.Left, policies) || Satisfies(logical.Right, policies),
        _ => false
    };

    static IEnumerable<(string Artifact, SemanticAuthorization Requirement)> Requirements(ExecutableSemanticModel model)
    {
        var slices = SemanticHostModelWalker.Slices(model).ToArray();
        var readModels = slices.SelectMany(located => located.Slice.ReadModels)
            .ToDictionary(readModel => readModel.Id, readModel => ModelNaming.ToIdentifier(readModel.Name));
        foreach (var located in slices)
        {
            foreach (var command in located.Slice.Commands.Where(command => command.Authorization is not null))
            {
                yield return ($"{located.TypeNamespace}.{ModelNaming.ToIdentifier(command.Name)}", command.Authorization!);
            }

            foreach (var query in located.Slice.Queries.Where(query => query.Authorization is not null))
            {
                // The HTTP surface places a query under the slice that declares it, even when its read model lives elsewhere.
                yield return ($"{located.TypeNamespace}.{readModels[query.ReadModel]}.{ModelNaming.ToIdentifier(query.Name)}", query.Authorization!);
            }
        }
    }

    static string Key(string method, string route) => $"{method} {route.Trim('/')}";
}
