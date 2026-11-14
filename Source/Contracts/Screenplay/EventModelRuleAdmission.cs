// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Stage.Contracts.Screenplay;

/// <summary>
/// Records modeled rules the legacy EventModel engine cannot enforce, without refusing other engines or renderers.
/// </summary>
public sealed class EventModelRuleAdmission : ScreenplaySyntaxWalker
{
    readonly List<string> _diagnostics = [];
    readonly HashSet<string> _enumConcepts = new(StringComparer.Ordinal);

    /// <summary>
    /// Finds rule declarations that would lose their semantics in legacy execution.
    /// </summary>
    /// <param name="syntax">The compiled application syntax.</param>
    /// <returns>The diagnostics retained for execution admission.</returns>
    public static IReadOnlyList<string> Inspect(ApplicationSyntax syntax)
    {
        var admission = new EventModelRuleAdmission();
        admission._enumConcepts.UnionWith(syntax.Concepts.Where(concept => concept.IsEnum).Select(concept => concept.Name));
        admission.VisitApplication(syntax);

        return admission._diagnostics;
    }

    /// <summary>
    /// Refuses a model before the legacy engine can register or execute it with unenforced rules.
    /// </summary>
    /// <param name="model">The converted event model.</param>
    /// <param name="path">The model's source path.</param>
    /// <exception cref="InvalidEventModel">The legacy engine cannot enforce an authored rule.</exception>
    public static void EnsureExecutable(EventModel model, string path)
    {
        if (model.UnsupportedEventModelRules.Count > 0)
        {
            throw new InvalidEventModel(path, model.UnsupportedEventModelRules);
        }
    }

    /// <inheritdoc/>
    public override void VisitNode(SyntaxNode node)
    {
        var declaration = UnsupportedDeclaration(node);
        if (declaration is not null)
        {
            _diagnostics.Add($"STAGE-ESM-081: Unsupported by the eventmodel engine: {declaration} at {node.Location}. Use the semantic engine or remove the unsupported declaration.");
        }
    }

    string? UnsupportedDeclaration(SyntaxNode node) => node switch
    {
        ConceptSyntax concept when concept.Validations?.Any() == true => $"concept '{concept.Name}' validation",
        ConstraintSyntax constraint => $"constraint '{constraint.Name}'",
        QuerySyntax query when query.Filters.Any() => $"query '{query.Name}' filter declarations",
        CommandSyntax command => UnsupportedCommandRule(command),
        _ => null
    };

    string? UnsupportedCommandRule(CommandSyntax command)
    {
        var enumProperty = command.Properties.FirstOrDefault(property => _enumConcepts.Contains(property.Type.Name));
        if (enumProperty is not null)
        {
            return $"enum concept '{enumProperty.Type.Name}' membership on command '{command.Name}.{enumProperty.Name}'";
        }

        return command.Validations.Any(DropsRules) ? $"command '{command.Name}' validation" : null;
    }

    static bool DropsRules(ValidateSyntax validation) =>
        validation is not DeclarativeValidateSyntax declarative ||
        ValidationRuleConverter.Convert([validation]).Sum(group => group.Rules.Count) != declarative.Rules.Count();
}
