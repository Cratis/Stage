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
        foreach (var component in visitor._components.Where(component => component.Component == "Cratis.Blueprint.Default.userMenu"))
        {
            var persona = personas.FirstOrDefault(persona => persona.Name == Property(component, "role"));
            if (persona is not null)
            {
                return (persona, Property(component, "name") ?? persona.Name);
            }
        }

        var first = personas.FirstOrDefault();
        return first is null ? null : (first, menu is null ? first.Name : Property(menu, "name") ?? first.Name);
    }

    static string? Property(ScreenComponentSyntax component, string name) =>
        (component.Properties.FirstOrDefault(property => property.Property == name)?.Value as LiteralExpressionSyntax)?.Value as string;
}
