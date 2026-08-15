// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Projections;
using Cratis.Stage.Rendering.Cratis.Authorization;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Expressions;
using Cratis.Stage.Rendering.Cratis.Naming;
using Cratis.Stage.Rendering.Cratis.Types;
using Cratis.Stage.Rendering.Cratis.Validation;

namespace Cratis.Stage.Rendering.Cratis.Renderers;

/// <summary>
/// Renders a <see cref="SliceType.StateChange"/> slice: the <c>[Command]</c> record, the authorization attribute
/// its <c>authorize</c> declares, its paired <c>CommandValidator&lt;T&gt;</c> when the command declares
/// validation, and the <c>[EventType]</c> records it can produce. Everything else the slice declares is reported
/// through <see cref="UnrenderedConstructs"/> rather than silently dropped.
/// </summary>
public class StateChangeSliceRenderer : ISliceRenderer
{
    /// <inheritdoc/>
    public RenderedFile Render(LocatedSlice slice, ApplicationSet applicationSet, string rootNamespace)
    {
        var diagnostics = new List<string>();
        var ownNamespace = SliceNaming.Namespace(rootNamespace, slice.FullPath);
        var builder = new CSharpCodeBuilder().Namespace(ownNamespace);

        UnrenderedConstructs.Report(builder, slice.Slice, RenderedConstructs.Command, diagnostics);

        var command = slice.Slice.Commands.FirstOrDefault();
        if (command is not null)
        {
            RenderCommand(builder, command, applicationSet, diagnostics);
        }

        foreach (var @event in slice.Slice.Events)
        {
            EventRenderer.Render(builder, @event, applicationSet, diagnostics);
        }

        foreach (var @namespace in ReferencedNamespaces.Resolve(ReferencedNames(slice.Slice), applicationSet, rootNamespace, ownNamespace))
        {
            builder.Using(@namespace);
        }

        var path = new List<string>(SliceNaming.FolderPath(slice.FullPath)) { SliceNaming.FileName(slice.Slice.Name) };
        return new RenderedFile(Path.Combine([.. path]), builder.ToString()) { Diagnostics = diagnostics };
    }

    static IEnumerable<string> ReferencedNames(SliceSyntax slice) =>
        slice.Commands.SelectMany(command => command.Properties).Select(property => property.Type.Name)
            .Concat(EventRenderer.ReferencedNames(slice.Events))
            .Concat(slice.Commands.SelectMany(command => command.Produces).Select(produces => produces.Event));

    static void RenderCommand(CSharpCodeBuilder builder, CommandSyntax command, ApplicationSet applicationSet, ICollection<string> diagnostics)
    {
        var typeName = Identifiers.ToPascalCase(command.Name);
        var parameters = string.Join(", ", command.Properties.Select(property => RenderParameter(property, command.Name, applicationSet, diagnostics)));
        var authorization = AuthorizationRenderer.Render(command.Authorize, applicationSet, $"Command '{command.Name}'", diagnostics);

        builder.Using("Cratis.Arc.Commands.ModelBound").Using(AuthorizationRenderer.Namespace);

        if (command.Properties.Any(property => property.IsIdentifier && TypeResolver.Resolve(property.Type, applicationSet).Kind != ResolvedTypeKind.Concept))
        {
            builder.Using("Cratis.Chronicle.Keys");
        }

        builder.BlankLine().Attribute("Command").Attribute(authorization).OpenBlock($"public record {typeName}({parameters})");

        CommandValidatorRenderer.Render(builder, command, typeName, applicationSet, diagnostics);
        RenderHandle(builder, command, applicationSet, diagnostics);

        builder.EndBlock();
    }

    static void RenderHandle(CSharpCodeBuilder builder, CommandSyntax command, ApplicationSet applicationSet, ICollection<string> diagnostics)
    {
        if (command.Handler?.Code is not null)
        {
            diagnostics.Add(
                $"Command '{command.Name}' handles with an authored {command.Handler.Code.Language} block, which is written against Screenplay's " +
                "own CommandContext — a rendered Arc handler receives Arc's, so the block is emitted as written and compiles only where the two agree.");
            builder.Using("Cratis.Arc.Commands")
                .BlankLine()
                .OpenBlock("public IEnumerable<object> Handle(CommandContext context)")
                .Raw(command.Handler.Code.Code)
                .EndBlock();
            return;
        }

        var produces = command.Produces.ToArray();

        if (produces.Length == 0)
        {
            builder.BlankLine().OpenBlock("public void Handle()").EndBlock();
            return;
        }

        // Every produced event is rendered before the signature is written, because rendering is what discovers
        // which collaborators the handler has to ask for — a `$context` path is reachable only through one.
        var context = new CommandContextAccess($"Command '{command.Name}'", diagnostics);
        var rendered = produces.Select(produced => (
            Event: Identifiers.ToPascalCase(produced.Event),
            Arguments: RenderEventArguments(produced, command, context, applicationSet, diagnostics),
            Condition: produced.When is null
                ? null
                : ExpressionRenderer.Render(produced.When, context, path => EnumTypeOfCommandProperty(path, command, applicationSet))))
            .ToArray();

        foreach (var @namespace in context.Namespaces)
        {
            builder.Using(@namespace);
        }

        var parameters = string.Join(", ", context.Collaborators.Select(collaborator => collaborator.ToParameter()));

        if (rendered.Length == 1 && rendered[0].Condition is null)
        {
            builder.BlankLine().ExpressionMember($"public {rendered[0].Event} Handle({parameters})", $"new({rendered[0].Arguments})");
            return;
        }

        builder.BlankLine().OpenBlock($"public IEnumerable<object> Handle({parameters})").Line("var events = new List<object>();");

        foreach (var (@event, arguments, condition) in rendered)
        {
            if (condition is not null)
            {
                builder.OpenBlock($"if ({condition})").Line($"events.Add(new {@event}({arguments}));").EndBlock();
            }
            else
            {
                builder.Line($"events.Add(new {@event}({arguments}));");
            }
        }

        builder.Line("return events;").EndBlock();
    }

    /// <summary>
    /// Renders the constructor arguments for a produced event. The argument list follows the <b>event's</b>
    /// declared property order — which is why the event is looked up across the whole application set rather than
    /// only the producing slice: a command routinely produces an event another slice declares, and falling back to
    /// the mapping order then constructs it with the wrong number of arguments.
    /// </summary>
    /// <param name="produces">The <c>produces</c> declaration to render arguments for.</param>
    /// <param name="command">The command producing the event — the scope a mapping source has to resolve against.</param>
    /// <param name="context">The <see cref="CommandContextAccess"/> rendering what reaches outside the command.</param>
    /// <param name="applicationSet">The <see cref="ApplicationSet"/> to resolve the event and its property types against.</param>
    /// <param name="diagnostics">Collects anything that could not be rendered faithfully.</param>
    /// <returns>The rendered argument list.</returns>
    static string RenderEventArguments(
        ProducesSyntax produces, CommandSyntax command, CommandContextAccess context, ApplicationSet applicationSet, ICollection<string> diagnostics)
    {
        var targetEvent = applicationSet.Events.GetValueOrDefault(produces.Event);
        if (targetEvent is null)
        {
            diagnostics.Add($"Event '{produces.Event}' is not declared in this application — constructed from the mapped values only.");
        }

        var properties = targetEvent?.Properties.ToArray();
        var propertyOrder = properties?.Select(property => property.Name) ?? produces.Mappings.Select(mapping => mapping.Property);

        var arguments = propertyOrder.Select(propertyName =>
        {
            var mapping = produces.Mappings.FirstOrDefault(candidate => string.Equals(candidate.Property, propertyName, StringComparison.OrdinalIgnoreCase));
            if (mapping is null)
            {
                return "default!";
            }

            var declared = properties?.FirstOrDefault(property => string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase));
            return RenderEventArgument(mapping, declared, command, context, applicationSet, diagnostics);
        });

        return string.Join(", ", arguments);
    }

    /// <summary>
    /// Renders one event constructor argument. A string literal assigned to a property typed as an enum concept is
    /// rendered as the enum member rather than the literal — the literal is what the Screenplay author wrote, but it
    /// is not assignable to the type the event declares. A source path the command does not carry is rendered as a
    /// missing value and reported, rather than as an identifier bound to nothing.
    /// </summary>
    /// <param name="mapping">The property mapping to render.</param>
    /// <param name="declared">The declared event property, when the event is known.</param>
    /// <param name="command">The command producing the event.</param>
    /// <param name="context">The <see cref="CommandContextAccess"/> rendering what reaches outside the command.</param>
    /// <param name="applicationSet">The <see cref="ApplicationSet"/> to resolve the property type against.</param>
    /// <param name="diagnostics">Collects anything that could not be rendered faithfully.</param>
    /// <returns>The rendered argument.</returns>
    static string RenderEventArgument(
        PropertyMappingSyntax mapping,
        PropertySyntax? declared,
        CommandSyntax command,
        CommandContextAccess context,
        ApplicationSet applicationSet,
        ICollection<string> diagnostics)
    {
        if (declared is not null && mapping.Source is LiteralExpressionSyntax { Value: string text })
        {
            var resolved = TypeResolver.Resolve(declared.Type, applicationSet);
            if (resolved.Kind == ResolvedTypeKind.Enum)
            {
                return $"{resolved.ClrTypeName}.{Identifiers.ToPascalCase(text)}";
            }
        }

        if (mapping.Source is PathExpressionSyntax path && CommandProperty(path.Path, command) is null)
        {
            diagnostics.Add($"'{mapping.Property}' is mapped from '{path.Path}', which command '{command.Name}' does not carry — rendered as a missing value.");
            return "default!";
        }

        if (mapping.Source is ContextExpressionSyntax contextExpression && Mismatch(contextExpression, declared, applicationSet) is { } mismatch)
        {
            diagnostics.Add(
                $"'{mapping.Property}' is mapped from '$context.{contextExpression.Path}', {mismatch} — rendered as a missing value.");
            return "default!";
        }

        return ExpressionRenderer.Render(mapping.Source, context);
    }

    /// <summary>
    /// Describes why a context value cannot fill the property the document maps it onto, or <see langword="null"/>
    /// when it can. The runtime carries every one of these as a fixed type — the tenant as a string, the caller's
    /// subject as a string — and a document is free to declare the property it fills as anything, so a
    /// <c>Uuid</c> concept fed from a string identifier is a mapping no rendering can honor.
    /// </summary>
    /// <param name="context">The context expression being mapped.</param>
    /// <param name="declared">The declared event property, when the event is known.</param>
    /// <param name="applicationSet">The <see cref="ApplicationSet"/> to resolve the property type against.</param>
    /// <returns>The description, or <see langword="null"/> when the value fits.</returns>
    static string? Mismatch(ContextExpressionSyntax context, PropertySyntax? declared, ApplicationSet applicationSet)
    {
        if (declared is null || CommandContextAccess.ValueTypeOf(context.Path) is not { } value)
        {
            return null;
        }

        var underlying = UnderlyingType(declared.Type, applicationSet);
        return underlying is null || underlying == value
            ? null
            : $"a {value} the runtime supplies, which the event declares as '{declared.Type.Name}' — a {underlying}";
    }

    /// <summary>
    /// Resolves the C# type a declared property ultimately holds — the primitive itself, or the one a concept
    /// wraps. An enum, a composite type or an unresolved name has none.
    /// </summary>
    /// <param name="type">The declared type reference.</param>
    /// <param name="applicationSet">The <see cref="ApplicationSet"/> to resolve against.</param>
    /// <returns>The C# type name, or <see langword="null"/> when the property holds no single primitive.</returns>
    static string? UnderlyingType(TypeRefSyntax type, ApplicationSet applicationSet)
    {
        var resolved = TypeResolver.Resolve(type, applicationSet);
        if (resolved.IsCollection)
        {
            return null;
        }

        return resolved.Kind switch
        {
            ResolvedTypeKind.Primitive => resolved.ClrTypeName,
            ResolvedTypeKind.Concept when applicationSet.Concepts.TryGetValue(type.Name, out var concept) =>
                TypeResolver.Resolve(new TypeRefSyntax(concept.Type, false, false, concept.Location), applicationSet) is
                    { Kind: ResolvedTypeKind.Primitive } underlying ? underlying.ClrTypeName : null,
            _ => null,
        };
    }

    static PropertySyntax? CommandProperty(string name, CommandSyntax command) =>
        command.Properties.FirstOrDefault(candidate => string.Equals(candidate.Name, name.Split('.')[0], StringComparison.OrdinalIgnoreCase));

    static string? EnumTypeOfCommandProperty(string path, CommandSyntax command, ApplicationSet applicationSet)
    {
        var property = CommandProperty(path, command);
        if (property is null)
        {
            return null;
        }

        var resolved = TypeResolver.Resolve(property.Type, applicationSet);
        return resolved.Kind == ResolvedTypeKind.Enum ? resolved.ClrTypeName : null;
    }

    static string RenderParameter(PropertySyntax property, string commandName, ApplicationSet applicationSet, ICollection<string> diagnostics)
    {
        var resolved = TypeResolver.Resolve(property.Type, applicationSet);
        var diagnostic = TypeResolver.DescribeIfUnresolved(resolved, $"property '{property.Name}' of command '{commandName}'");
        if (diagnostic is not null)
        {
            diagnostics.Add(diagnostic);
        }

        var prefix = property.IsIdentifier && resolved.Kind != ResolvedTypeKind.Concept ? "[Key] " : string.Empty;
        return $"{prefix}{resolved.ToTypeSyntax()} {Identifiers.ToPascalCase(property.Name)}";
    }
}
