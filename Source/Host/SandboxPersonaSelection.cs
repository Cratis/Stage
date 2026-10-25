// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Stage.Host;

internal sealed class SandboxPersonaSelection : ScreenplaySyntaxWalker
{
    readonly List<ScreenComponentSyntax> _components = [];

    public override void VisitScreenComponent(ScreenComponentSyntax syntax)
    {
        _components.Add(syntax);
        base.VisitScreenComponent(syntax);
    }

    internal static (PersonaSyntax Persona, string DisplayName)? Select(ApplicationSyntax application)
    {
        var visitor = new SandboxPersonaSelection();
        visitor.VisitApplication(application);
        var personas = application.Personas?.ToArray() ?? [];
        var menu = visitor._components.FirstOrDefault(component => component.Component == "Cratis.Blueprint.Default.userMenu");
        if (MenuPersona(application) is { } selection)
        {
            return selection;
        }

        var chosen = ModulePersona(application, personas) ?? personas.FirstOrDefault();
        return chosen is null ? null : (chosen, menu is null ? chosen.Name : Property(menu, "name") ?? chosen.Name);
    }

    internal static (PersonaSyntax Persona, string DisplayName)? MenuPersona(ApplicationSyntax application)
    {
        var visitor = new SandboxPersonaSelection();
        visitor.VisitApplication(application);
        var personas = application.Personas?.ToArray() ?? [];
        foreach (var component in visitor._components.Where(component => component.Component == "Cratis.Blueprint.Default.userMenu"))
        {
            var persona = personas.FirstOrDefault(persona => persona.Name == Property(component, "role"));
            if (persona is not null)
            {
                return (persona, Property(component, "name") ?? persona.Name);
            }
        }

        return null;
    }

    static PersonaSyntax? ModulePersona(ApplicationSyntax application, PersonaSyntax[] personas)
    {
        var requirements = application.Modules
            .Where(module => module.Authorize is not null)
            .Select(module => module.Authorize!.Requirement)
            .ToArray();
        return personas
            .Select((persona, index) => (persona, index, admitted: requirements.Count(requirement => Satisfies(requirement, persona.Policies.ToHashSet(StringComparer.Ordinal)))))
            .Where(candidate => candidate.admitted > 0)
            .OrderByDescending(candidate => candidate.admitted)
            .ThenBy(candidate => candidate.index)
            .Select(candidate => candidate.persona)
            .FirstOrDefault();
    }

    static bool Satisfies(PolicyRequirementSyntax requirement, HashSet<string> policies) => requirement switch
    {
        PolicyReferenceSyntax reference => policies.Contains(reference.Name),
        LogicalPolicyRequirementSyntax { Operator: LogicalOperator.And } logical => Satisfies(logical.Left, policies) && Satisfies(logical.Right, policies),
        LogicalPolicyRequirementSyntax { Operator: LogicalOperator.Or } logical => Satisfies(logical.Left, policies) || Satisfies(logical.Right, policies),
        _ => false
    };

    static string? Property(ScreenComponentSyntax component, string name) =>
        (component.Properties.FirstOrDefault(property => property.Property == name)?.Value as LiteralExpressionSyntax)?.Value as string;
}
