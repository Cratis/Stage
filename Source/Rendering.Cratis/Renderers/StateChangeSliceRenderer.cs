// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Projections;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Expressions;
using Cratis.Stage.Rendering.Cratis.Naming;
using Cratis.Stage.Rendering.Cratis.Types;
using Cratis.Stage.Rendering.Cratis.Validation;

namespace Cratis.Stage.Rendering.Cratis.Renderers;

/// <summary>
/// Renders a <see cref="SliceType.StateChange"/> slice: the <c>[Command]</c> record, its paired
/// <c>CommandValidator&lt;T&gt;</c> when the command declares validation, and the <c>[EventType]</c> records it
/// can produce.
/// </summary>
public class StateChangeSliceRenderer : ISliceRenderer
{
    /// <inheritdoc/>
    public RenderedFile Render(LocatedSlice slice, ApplicationSet applicationSet, string rootNamespace)
    {
        var builder = new CSharpCodeBuilder().Namespace(SliceNaming.Namespace(rootNamespace, slice.FullPath));

        foreach (var @using in ConceptUsings(slice, applicationSet, rootNamespace))
        {
            builder.Using(@using);
        }

        var command = slice.Slice.Commands.FirstOrDefault();
        if (command is not null)
        {
            RenderCommand(builder, slice.Slice, command, applicationSet);
        }

        foreach (var @event in slice.Slice.Events)
        {
            RenderEvent(builder, @event, applicationSet);
        }

        var path = new List<string>(SliceNaming.FolderPath(slice.FullPath)) { SliceNaming.FileName(slice.Slice.Name) };
        return new RenderedFile(Path.Combine([.. path]), builder.ToString());
    }

    static void RenderCommand(CSharpCodeBuilder builder, SliceSyntax slice, CommandSyntax command, ApplicationSet applicationSet)
    {
        var typeName = Identifiers.ToPascalCase(command.Name);
        var parameters = string.Join(", ", command.Properties.Select(property => RenderParameter(property, applicationSet, isCommand: true)));
        var requiresContext = RequiresContext(command);

        builder.Using("Cratis.Arc.Commands.ModelBound");
        if (requiresContext || command.Handler?.Code is not null)
        {
            builder.Using("Cratis.Arc.Commands");
        }

        if (command.Properties.Any(property => property.IsIdentifier && TypeResolver.Resolve(property.Type, applicationSet).Kind != ResolvedTypeKind.Concept))
        {
            builder.Using("Cratis.Chronicle.Keys");
        }

        builder.BlankLine().Attribute("Command").OpenBlock($"public record {typeName}({parameters})");

        RenderValidator(builder, command, typeName, applicationSet);
        RenderHandle(builder, slice, command, requiresContext);

        builder.EndBlock();
    }

    static void RenderValidator(CSharpCodeBuilder builder, CommandSyntax command, string typeName, ApplicationSet applicationSet)
    {
        var rules = command.Validations.OfType<DeclarativeValidateSyntax>().SelectMany(validate => validate.Rules).ToArray();
        if (rules.Length == 0)
        {
            return;
        }

        var validatorName = $"{typeName}Validator";
        var ruleMethods = new List<(string Name, string ParameterType, string PropertyName, string Code)>();

        builder.BlankLine().OpenBlock($"public class {validatorName} : CommandValidator<{typeName}>").OpenBlock($"public {validatorName}()");

        foreach (var rule in rules)
        {
            RenderRule(builder, rule, command, applicationSet, ruleMethods);
        }

        builder.EndBlock();

        foreach (var method in ruleMethods)
        {
            builder.BlankLine().OpenBlock($"static bool {method.Name}({method.ParameterType} {method.PropertyName})").Raw(method.Code).EndBlock();
        }

        builder.EndBlock();
    }

    static void RenderRule(
        CSharpCodeBuilder builder,
        ValidationRuleSyntax rule,
        CommandSyntax command,
        ApplicationSet applicationSet,
        List<(string Name, string ParameterType, string PropertyName, string Code)> ruleMethods)
    {
        var property = Identifiers.ToPascalCase(rule.Property);
        var value = rule.Value is null ? string.Empty : ExpressionRenderer.Render(rule.Value);
        var call = rule.Rule == ValidationRuleKind.Rule && rule.Code is not null
            ? RenderCustomRule(rule, property, command, applicationSet, ruleMethods)
            : ValidationRuleRenderer.RenderCall(rule.Rule, value);

        if (call is null)
        {
            builder.Line($"// TODO: unsupported validation rule '{rule.Rule}' on '{rule.Property}'");
            return;
        }

        builder.Line($"RuleFor(_ => _.{property}){call}{ValidationRuleRenderer.RenderMessage(rule)};");
    }

    static string RenderCustomRule(
        ValidationRuleSyntax rule,
        string property,
        CommandSyntax command,
        ApplicationSet applicationSet,
        List<(string Name, string ParameterType, string PropertyName, string Code)> ruleMethods)
    {
        var commandProperty = command.Properties.FirstOrDefault(candidate => string.Equals(candidate.Name, rule.Property, StringComparison.OrdinalIgnoreCase));
        var parameterType = commandProperty is null ? "string" : TypeResolver.Resolve(commandProperty.Type, applicationSet).ToTypeSyntax();

        var methodName = $"Satisfy{property}Rule{ruleMethods.Count(method => method.Name.StartsWith($"Satisfy{property}Rule", StringComparison.Ordinal)) + 1}";
        ruleMethods.Add((methodName, parameterType, property, rule.Code!.Code));
        return $".Must({methodName})";
    }

    static void RenderHandle(CSharpCodeBuilder builder, SliceSyntax slice, CommandSyntax command, bool requiresContext)
    {
        var contextParameter = requiresContext ? "CommandContext context" : string.Empty;

        if (command.Handler?.Code is not null)
        {
            builder.BlankLine().OpenBlock("public IEnumerable<object> Handle(CommandContext context)").Raw(command.Handler.Code.Code).EndBlock();
            return;
        }

        var produces = command.Produces.ToArray();

        if (produces.Length == 0)
        {
            builder.BlankLine().OpenBlock($"public void Handle({contextParameter})").EndBlock();
            return;
        }

        if (produces.Length == 1 && produces[0].When is null)
        {
            var eventTypeName = Identifiers.ToPascalCase(produces[0].Event);
            builder.BlankLine().ExpressionMember(
                $"public {eventTypeName} Handle({contextParameter})",
                $"new({RenderEventArguments(slice, produces[0])})");
            return;
        }

        builder.BlankLine().OpenBlock($"public IEnumerable<object> Handle({contextParameter})").Line("var events = new List<object>();");

        foreach (var produced in produces)
        {
            var eventTypeName = Identifiers.ToPascalCase(produced.Event);
            var arguments = RenderEventArguments(slice, produced);

            if (produced.When is not null)
            {
                builder.OpenBlock($"if ({ExpressionRenderer.Render(produced.When)})")
                    .Line($"events.Add(new {eventTypeName}({arguments}));")
                    .EndBlock();
            }
            else
            {
                builder.Line($"events.Add(new {eventTypeName}({arguments}));");
            }
        }

        builder.Line("return events;").EndBlock();
    }

    static string RenderEventArguments(SliceSyntax slice, ProducesSyntax produces)
    {
        var targetEvent = slice.Events.FirstOrDefault(candidate => candidate.Name == produces.Event);
        var propertyOrder = targetEvent?.Properties.Select(property => property.Name) ?? produces.Mappings.Select(mapping => mapping.Property);

        var arguments = propertyOrder.Select(propertyName =>
        {
            var mapping = produces.Mappings.FirstOrDefault(candidate => string.Equals(candidate.Property, propertyName, StringComparison.OrdinalIgnoreCase));
            return mapping is not null ? ExpressionRenderer.Render(mapping.Source) : "default!";
        });

        return string.Join(", ", arguments);
    }

    static void RenderEvent(CSharpCodeBuilder builder, EventSyntax @event, ApplicationSet applicationSet)
    {
        var typeName = Identifiers.ToPascalCase(@event.Name);
        var parameters = string.Join(", ", @event.Properties.Select(property => RenderParameter(property, applicationSet, isCommand: false)));

        builder.BlankLine().Using("Cratis.Chronicle.Events").Summary($"Emitted when {Identifiers.ToWords(@event.Name)}.");
        foreach (var property in @event.Properties)
        {
            builder.Line($"/// <param name=\"{Identifiers.ToPascalCase(property.Name)}\">The {Identifiers.ToWords(property.Name)}.</param>");
        }

        builder.Attribute("EventType").Line($"public record {typeName}({parameters});");
    }

    static string RenderParameter(PropertySyntax property, ApplicationSet applicationSet, bool isCommand)
    {
        var resolved = TypeResolver.Resolve(property.Type, applicationSet);
        var prefix = isCommand && property.IsIdentifier && resolved.Kind != ResolvedTypeKind.Concept ? "[Key] " : string.Empty;
        return $"{prefix}{resolved.ToTypeSyntax()} {Identifiers.ToPascalCase(property.Name)}";
    }

    static IReadOnlyList<string> ConceptUsings(LocatedSlice slice, ApplicationSet applicationSet, string rootNamespace)
    {
        var ownNamespace = SliceNaming.Namespace(rootNamespace, slice.FullPath);
        var namespaces = new HashSet<string>(StringComparer.Ordinal);

        var referencedNames = slice.Slice.Commands.SelectMany(command => command.Properties)
            .Concat(slice.Slice.Events.SelectMany(@event => @event.Properties))
            .Select(property => property.Type.Name)
            .Where(name => applicationSet.Concepts.ContainsKey(name) || applicationSet.Types.ContainsKey(name));

        foreach (var name in referencedNames)
        {
            var placement = applicationSet.ConceptPlacements.GetValueOrDefault(name, []);
            var @namespace = placement.Count == 0 ? $"{rootNamespace}.Common" : SliceNaming.Namespace(rootNamespace, placement);
            if (!string.Equals(@namespace, ownNamespace, StringComparison.Ordinal))
            {
                namespaces.Add(@namespace);
            }
        }

        return [.. namespaces];
    }

    static bool RequiresContext(CommandSyntax command) => command.Produces.Any(produces =>
        (produces.When is not null && ConditionReferencesContext(produces.When)) ||
        produces.Mappings.Any(mapping => ExpressionReferencesContext(mapping.Source)));

    static bool ExpressionReferencesContext(ExpressionSyntax expression) => expression switch
    {
        ContextExpressionSyntax or EventContextExpressionSyntax or CausedByExpressionSyntax or EventSourceIdExpressionSyntax => true,
        TemplateExpressionSyntax template => template.Parts.OfType<TemplateInterpolationSyntax>().Any(part => ExpressionReferencesContext(part.Expression)),
        _ => false,
    };

    static bool ConditionReferencesContext(ConditionSyntax condition) => condition switch
    {
        ComparisonConditionSyntax comparison => ExpressionReferencesContext(comparison.Right),
        LogicalConditionSyntax logical => ConditionReferencesContext(logical.Left) || ConditionReferencesContext(logical.Right),
        _ => false,
    };
}
