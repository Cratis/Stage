// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Stage.Host;

internal static class SandboxPersona
{
    internal static ClaimsPrincipal? Create(ApplicationSyntax application, ILogger logger)
    {
        if (SandboxPersonaSelection.Select(application) is not { } selection)
        {
            return null;
        }

        var principal = Create(selection.Persona, selection.DisplayName, application, logger);
        StageLog.SandboxPersonaSelected(logger, selection.Persona.Name, string.Join(", ", principal.FindAll(ClaimTypes.Role).Select(claim => claim.Value)));
        return principal;
    }

    internal static ClaimsPrincipal Create(PersonaSyntax persona, string displayName, ApplicationSyntax application, ILogger logger) =>
        Principal(persona, displayName, CallerFor(persona, application, logger));

    static ClaimsPrincipal Principal(PersonaSyntax persona, string displayName, SpecificationCallerSyntax? caller)
    {
        var subject = $"persona:{persona.Name}";
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, subject),
            new("sub", subject),
            new(ClaimTypes.Name, displayName)
        };
        claims.AddRange((caller?.Roles ?? []).Select(role => new Claim(ClaimTypes.Role, role)));
        claims.AddRange((caller?.Claims ?? []).Select(claim => new Claim(claim.Type, claim.Value)));
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "stage-persona"));
    }

    static SpecificationCallerSyntax? CallerFor(PersonaSyntax persona, ApplicationSyntax application, ILogger logger)
    {
        var supported = new List<string>();
        SpecificationCallerSyntax? caller = null;
        foreach (var policy in persona.Policies)
        {
            var condition = application.Policies.FirstOrDefault(candidate => candidate.Name == policy)?.Condition;
            var result = PersonaCallers.Synthesize(persona with { Policies = [.. supported, policy] }, application);
            var reason = RequiresAnonymous(condition) ? "anonymousRequirement" : result.Refusal?.Reason;
            if (reason is not null)
            {
                StageLog.SandboxPersonaPolicySkipped(logger, persona.Name, policy, reason);
                continue;
            }

            supported.Add(policy);
            caller = result.Caller;
        }

        return caller;
    }

    static bool RequiresAnonymous(PolicyConditionSyntax? condition) => condition switch
    {
        AnonymousConditionSyntax => true,
        LogicalPolicyConditionSyntax logical => RequiresAnonymous(logical.Left) || RequiresAnonymous(logical.Right),
        _ => false
    };
}
