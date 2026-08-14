// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Specifications;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Naming;

namespace Cratis.Stage.Rendering.Cratis.Specifications;

/// <summary>
/// Renders a Screenplay <c>specification</c> as a Cratis spec — a <c>CommandScenario&lt;T&gt;</c> exercising the
/// slice's own command and asserting on what it appended.
/// </summary>
/// <remarks>
/// <para>
/// One file per specification, in the folder layout the repository conventions use, wrapped in <c>#if DEBUG</c>
/// so spec code ships only in Debug. A Screenplay specification carries a single name rather than a
/// behavior/outcome pair, so it renders as a single <c>when_</c> file rather than being split into a hierarchy
/// the document never stated.
/// </para>
/// <para>
/// A specification declaring <c>given</c> is not rendered at all — see <see cref="Unrenderable"/>. Rendering it
/// without its prior state would produce a spec that passes or fails for reasons the document did not state,
/// which is worse than not having it.
/// </para>
/// </remarks>
public static class SpecificationRenderer
{
    /// <summary>
    /// Says why a specification cannot be rendered faithfully, or <see langword="null"/> when it can.
    /// </summary>
    /// <param name="specification">The specification to consider.</param>
    /// <param name="slice">The slice the specification belongs to.</param>
    /// <returns>The reason, or <see langword="null"/>.</returns>
    /// <remarks>
    /// The <c>given</c> case is the interesting one, and it is not a gap in the target: <c>CommandScenario</c>
    /// gained <c>Given.ForEventSource(id).Events(…)</c>. It is a gap in the <i>document</i> — a <c>given</c>
    /// event names no event source, and which one it belongs to is not recoverable. It is frequently not the
    /// command's own: a specification asserting that a duplicate invoice number is rejected seeds an
    /// <c>InvoiceRegistered</c> for a <i>different</i> invoice than the one the command registers, and seeding it
    /// against the command's id would make the spec assert something else entirely.
    /// </remarks>
    public static string? Unrenderable(SpecificationSyntax specification, SliceSyntax slice)
    {
        var command = slice.Commands.FirstOrDefault();
        if (command is null || specification.When is null)
        {
            return "it exercises no command, and only a command scenario is rendered";
        }

        if (!string.Equals(specification.When.CommandType, command.Name, StringComparison.OrdinalIgnoreCase))
        {
            // The slice may well declare it — only the first command is rendered, so anything else has no type to
            // exercise. Saying "this slice does not declare it" would send a reader to the wrong document.
            return slice.Commands.Any(candidate => string.Equals(candidate.Name, specification.When.CommandType, StringComparison.OrdinalIgnoreCase))
                ? $"it exercises '{specification.When.CommandType}', and only the first command a slice declares is rendered"
                : $"it exercises '{specification.When.CommandType}', which this slice does not declare";
        }

        if (specification.Given.Any())
        {
            return "it establishes prior state with 'given', and the document does not say which event source " +
                "those events belong to — seeding them against the command's own would assert something the " +
                "document did not state";
        }

        if (specification.GivenReadModels?.Any() == true || specification.ThenReadModels?.Any() == true)
        {
            return "it states read model state, which has no assertion in the scenario family";
        }

        if (specification.ThenEvents.Any() && specification.ThenErrors.Any())
        {
            return "it expects both a rejection and appended events, and a rejected command appends nothing — " +
                "rendering either half would assert something the document did not state";
        }

        if (!specification.ThenEvents.Any() && !specification.ThenErrors.Any())
        {
            return "it asserts nothing";
        }

        return null;
    }

    /// <summary>
    /// Renders a specification.
    /// </summary>
    /// <param name="specification">The specification to render.</param>
    /// <param name="command">The command the slice declares.</param>
    /// <param name="slice">The located slice the specification belongs to.</param>
    /// <param name="applicationSet">The <see cref="ApplicationSet"/> to resolve types against.</param>
    /// <param name="rootNamespace">The root namespace of the target application.</param>
    /// <returns>The <see cref="RenderedFile"/>.</returns>
    public static RenderedFile Render(
        SpecificationSyntax specification,
        CommandSyntax command,
        LocatedSlice slice,
        ApplicationSet applicationSet,
        string rootNamespace)
    {
        var diagnostics = new List<string>();
        var name = Behavior(specification.Name);
        var commandType = Identifiers.ToPascalCase(command.Name);
        var ownNamespace = $"{SliceNaming.Namespace(rootNamespace, slice.FullPath)}.{name}";
        var builder = new CSharpCodeBuilder()
            .Namespace(ownNamespace)
            .Using("Cratis.Arc.Testing.Commands")
            .Using("Cratis.Specifications")
            .Using("Xunit");

        // Resolved the way every other renderer resolves: a spec sits in a child namespace of its slice, so the
        // command it exercises is found without an import but an event another slice declares - or a concept
        // placed above the slice - is not.
        foreach (var @namespace in ReferencedNamespaces.Resolve(
            ReferencedNames(specification, command), applicationSet, rootNamespace, ownNamespace))
        {
            builder.Using(@namespace);
        }

        builder.BlankLine().OpenBlock($"public class {name} : Specification")
            .Line($"readonly CommandScenario<{commandType}> _scenario = new();")
            .Line("CommandResult _result = null!;")
            .BlankLine()
            .Line($"async Task Because() => _result = await _scenario.Execute(new {commandType}({Arguments(specification.When!, command, specification, applicationSet, diagnostics)}));")
            .BlankLine();

        if (specification.ThenErrors.Any())
        {
            RenderRejection(builder, specification, diagnostics);
        }
        else
        {
            RenderAppends(builder, specification, command, commandType, applicationSet, diagnostics);
        }

        builder.EndBlock();

        // Decided from the rendered content rather than predicted: a date or timestamp anywhere in the values or
        // the assertions renders as a parse against the invariant culture, and only the emitted text knows whether
        // one is there.
        var content = builder.ToString();
        if (SpecificationValues.NeedsGlobalization(content))
        {
            content = builder.Using("System.Globalization").ToString();
        }

        var path = new List<string>(SliceNaming.FolderPath(slice.FullPath)) { $"{name}.cs" };
        return new RenderedFile(Path.Combine([.. path]), Conditional(content)) { Diagnostics = diagnostics };
    }

    /// <summary>
    /// Renders the assertions for a rejected command. Both are emitted deliberately: on its own
    /// <c>ShouldNotBeSuccessful</c> cannot tell a validation rejection from an unhandled exception. A message the
    /// document states is not asserted on — the conventions hold that message strings are presentation text.
    /// </summary>
    /// <param name="builder">The <see cref="CSharpCodeBuilder"/> to emit to.</param>
    /// <param name="specification">The specification being rendered.</param>
    /// <param name="diagnostics">Collects anything that could not be rendered faithfully.</param>
    static void RenderRejection(CSharpCodeBuilder builder, SpecificationSyntax specification, List<string> diagnostics)
    {
        var named = specification.ThenErrors.Where(error => !string.IsNullOrWhiteSpace(error.Name)).ToArray();
        if (named.Length > 0)
        {
            diagnostics.Add(
                $"Specification '{specification.Name}' names {named.Length} expected rejection(s), which is not " +
                "asserted on — the conventions hold that a rejection's text is presentation, and the specification's " +
                "own name is where the reason belongs.");
        }

        builder.Line("[Fact] void should_not_succeed() => _result.ShouldNotBeSuccessful();")
            .Line("[Fact] void should_have_validation_errors() => _result.ShouldHaveValidationErrors();");
    }

    static void RenderAppends(
        CSharpCodeBuilder builder,
        SpecificationSyntax specification,
        CommandSyntax command,
        string commandType,
        ApplicationSet applicationSet,
        List<string> diagnostics)
    {
        builder.Using("Cratis.Arc.Chronicle.Testing.Commands").Line("[Fact] void should_succeed() => _result.ShouldBeSuccessful();");

        var identifier = SpecificationAssertions.Of(specification.When!, command, applicationSet, diagnostics);
        var events = specification.ThenEvents.ToArray();

        foreach (var (@event, index) in events.Select((@event, index) => (@event, index)))
        {
            var eventType = Identifiers.ToPascalCase(@event.EventType);
            var predicate = SpecificationAssertions.Predicate(@event, applicationSet, diagnostics);

            // A fanout states the same event type more than once, each with its own values. The facts have to be
            // named apart or the rendered class declares the same member twice.
            var occurrence = events.Count(other => string.Equals(other.EventType, @event.EventType, StringComparison.OrdinalIgnoreCase)) > 1
                ? $"_{events.Take(index + 1).Count(other => string.Equals(other.EventType, @event.EventType, StringComparison.OrdinalIgnoreCase))}"
                : string.Empty;

            builder.Line(
                $"[Fact] async Task should_have_appended_{Identifiers.ToSnakeCase(@event.EventType)}{occurrence}() => " +
                $"await _scenario.ShouldHaveAppendedEvent<{commandType}, {eventType}>({identifier}{predicate});");
        }
    }

    /// <summary>
    /// Renders the command's constructor arguments from the values the specification states. A property the
    /// specification says nothing about is constructed as a missing value and reported — the rendered spec then
    /// exercises a command the document only partly described, which is worth knowing when it fails.
    /// </summary>
    /// <param name="when">The command the specification exercises.</param>
    /// <param name="command">The declared command.</param>
    /// <param name="specification">The specification being rendered, for diagnostics.</param>
    /// <param name="applicationSet">The <see cref="ApplicationSet"/> to resolve types against.</param>
    /// <param name="diagnostics">Collects anything that could not be rendered faithfully.</param>
    /// <returns>The rendered argument list.</returns>
    static string Arguments(
        SpecificationCommandSyntax when,
        CommandSyntax command,
        SpecificationSyntax specification,
        ApplicationSet applicationSet,
        List<string> diagnostics)
    {
        var unstated = command.Properties
            .Where(property => !when.Values.Any(value => string.Equals(value.Property, property.Name, StringComparison.OrdinalIgnoreCase)))
            .Select(property => property.Name)
            .ToArray();

        if (unstated.Length > 0)
        {
            diagnostics.Add(
                $"Specification '{specification.Name}' states no value for {string.Join(", ", unstated.Select(name => $"'{name}'"))} " +
                $"of command '{command.Name}' — the rendered spec constructs them as missing values.");
        }

        return string.Join(", ", command.Properties.Select(property => SpecificationValues.For(property, when.Values, command.Name, applicationSet, diagnostics)));
    }

    /// <summary>
    /// Every Screenplay name the rendered spec references — the command, the events it expects, and the types
    /// their values are stated as.
    /// </summary>
    /// <param name="specification">The specification being rendered.</param>
    /// <param name="command">The command it exercises.</param>
    /// <returns>The referenced names.</returns>
    static IEnumerable<string> ReferencedNames(SpecificationSyntax specification, CommandSyntax command) =>
        specification.ThenEvents.Select(@event => @event.EventType)
            .Append(command.Name)
            .Concat(command.Properties.Select(property => property.Type.Name));

    /// <summary>
    /// Turns the specification's name into the behavior the folder and class read as — <c>RegisteringADraftInvoice</c>
    /// becomes <c>when_registering_a_draft_invoice</c>.
    /// </summary>
    /// <param name="name">The declared specification name.</param>
    /// <returns>The rendered behavior name.</returns>
    static string Behavior(string name) => $"when_{Identifiers.ToSnakeCase(name)}";

    static string Conditional(string content) => $"#if DEBUG{Environment.NewLine}{content}{Environment.NewLine}#endif{Environment.NewLine}";
}
