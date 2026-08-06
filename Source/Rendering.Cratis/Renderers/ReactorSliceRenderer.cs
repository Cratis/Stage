// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Naming;

namespace Cratis.Stage.Rendering.Cratis.Renderers;

/// <summary>
/// Renders an <see cref="SliceType.Automation"/> or <see cref="SliceType.Translate"/> slice: the
/// <c>[EventType]</c> records the slice declares, plus one <c>IReactor</c> class per declared
/// <see cref="ReactorSyntax"/>, with one method per <see cref="ReactorTriggerSyntax"/>. Both slice types render
/// identically — Screenplay expresses their behavior the same way, as reactors reacting to events — so this
/// single renderer is registered for both.
/// </summary>
/// <remarks>
/// A trigger with an inline <c>csharp</c> block is embedded verbatim, preceded by a positional deconstruction of
/// the triggering event so the block can reference its properties by bare name (matching the authored
/// convention observed in Screenplay's own samples). A trigger with a <c>file</c> reference is stubbed — the
/// referenced file is not read or copied in this pass. A trigger with neither is Screenplay's own documented
/// "statement of intent" — a stub is emitted, not an error.
/// </remarks>
public class ReactorSliceRenderer : ISliceRenderer
{
    /// <inheritdoc/>
    public RenderedFile Render(LocatedSlice slice, ApplicationSet applicationSet, string rootNamespace)
    {
        var diagnostics = new List<string>();
        var ownNamespace = SliceNaming.Namespace(rootNamespace, slice.FullPath);
        var builder = new CSharpCodeBuilder()
            .Namespace(ownNamespace)
            .Using("System")
            .Using("Cratis.Chronicle.Events")
            .Using("Cratis.Chronicle.Reactors");

        UnrenderedConstructs.Report(builder, slice.Slice, RenderedConstructs.Reactors, diagnostics);

        foreach (var @event in slice.Slice.Events)
        {
            EventRenderer.Render(builder, @event, applicationSet, diagnostics);
        }

        foreach (var reactor in slice.Slice.Reactors)
        {
            RenderReactor(builder, reactor, slice.Slice);
        }

        foreach (var @namespace in ReferencedNamespaces.Resolve(ReferencedNames(slice.Slice), applicationSet, rootNamespace, ownNamespace))
        {
            builder.Using(@namespace);
        }

        var path = new List<string>(SliceNaming.FolderPath(slice.FullPath)) { SliceNaming.FileName(slice.Slice.Name) };
        return new RenderedFile(Path.Combine([.. path]), builder.ToString()) { Diagnostics = diagnostics };
    }

    static IEnumerable<string> ReferencedNames(SliceSyntax slice) =>
        EventRenderer.ReferencedNames(slice.Events)
            .Concat(slice.Reactors.SelectMany(reactor => reactor.Triggers).Select(trigger => trigger.Event));

    static void RenderReactor(CSharpCodeBuilder builder, ReactorSyntax reactor, SliceSyntax slice)
    {
        var typeName = Identifiers.ToPascalCase(reactor.Name);
        var summary = reactor.Description ?? $"Reacts to events for {Identifiers.ToWords(reactor.Name)}.";

        builder.BlankLine().Summary(summary).OpenBlock($"public class {typeName} : IReactor");

        var isFirst = true;
        foreach (var trigger in reactor.Triggers)
        {
            if (!isFirst)
            {
                builder.BlankLine();
            }

            isFirst = false;
            RenderTrigger(builder, trigger, slice);
        }

        builder.EndBlock();
    }

    static void RenderTrigger(CSharpCodeBuilder builder, ReactorTriggerSyntax trigger, SliceSyntax slice)
    {
        var eventTypeName = Identifiers.ToPascalCase(trigger.Event);
        if (trigger.Description is not null)
        {
            builder.Summary(trigger.Description);
        }

        builder.OpenBlock($"public IEnumerable<object>? {eventTypeName}({eventTypeName} @event, EventContext context)");

        if (trigger.Code is not null)
        {
            var triggeringEvent = slice.Events.FirstOrDefault(candidate => candidate.Name == trigger.Event);
            var propertyNames = triggeringEvent?.Properties.Select(property => Identifiers.ToPascalCase(property.Name)).ToArray() ?? [];
            if (propertyNames.Length > 0)
            {
                builder.Line($"var ({string.Join(", ", propertyNames)}) = @event;");
            }

            builder.Raw(trigger.Code.Code);
        }
        else if (trigger.File is not null)
        {
            builder.Line($"// TODO: implementation lives in '{trigger.File.Path}' — not embedded in this pass").Line("throw new NotImplementedException();");
        }
        else
        {
            builder.Line($"// TODO: implement the reaction to {eventTypeName}").Line("return null;");
        }

        builder.EndBlock();
    }
}
