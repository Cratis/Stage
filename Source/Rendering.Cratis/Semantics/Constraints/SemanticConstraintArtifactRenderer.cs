// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Naming;

namespace Cratis.Stage.Rendering.Cratis.Semantics.Constraints;

/// <summary>
/// Renders each portable append-time constraint as a named Chronicle constraint.
/// </summary>
internal static class SemanticConstraintArtifactRenderer
{
    internal static RenderedFile Render(SemanticSlice slice, SemanticConstraint constraint, SemanticApplicationContext context)
    {
        var location = context.Slice(slice.Id);
        var builder = new CSharpCodeBuilder()
            .Namespace(SliceNaming.Namespace(context.RootNamespace, location.Path))
            .Using("Cratis.Chronicle.Events.Constraints");
        var className = Identifiers.ToPascalCase(constraint.Name);
        var message = constraint.Message ?? (constraint.Kind == SemanticConstraintKind.UniquePropertyValue
            ? $"Constraint '{constraint.Name}' is violated: another event source already holds the constrained value."
            : $"Constraint '{constraint.Name}' is violated: the event source already has the constrained event.");

        builder.Summary($"Enforces the {constraint.Name} append-time constraint.")
            .OpenBlock($"public class {className} : IConstraint")
            .Line("/// <inheritdoc/>")
            .OpenBlock("public void Define(IConstraintBuilder builder)");

        if (constraint.Kind == SemanticConstraintKind.UniquePropertyValue)
        {
            builder.OpenBlock("builder.Unique(unique =>")
                .Line($"unique.WithName({CSharpCodeBuilder.StringLiteral(constraint.Name)})");
            foreach (var target in constraint.Targets)
            {
                var @event = context.Events[target.EventContract];
                var eventType = TypeName(@event, context);
                var properties = target.Properties.Select(id => Identifiers.ToPascalCase(@event.Properties.Single(_ => _.Id == id).Name));
                builder.Line($"    .On<{eventType}>({string.Join(", ", properties.Select(property => $"@event => @event.{property}"))})");
            }

            foreach (var id in constraint.ReleasedBy)
            {
                builder.Line($"    .RemovedWith<{TypeName(context.Events[id], context)}>()");
            }

            if (constraint.IgnoreCasing)
            {
                builder.Line("    .IgnoreCasing()");
            }

            builder.Line($"    .WithMessage({CSharpCodeBuilder.StringLiteral(message)});")
                .EndBlock()
                .Line(");");
        }
        else
        {
            foreach (var target in constraint.Targets)
            {
                builder.Line($"builder.Unique<{TypeName(context.Events[target.EventContract], context)}>(message: {CSharpCodeBuilder.StringLiteral(message)}, name: {CSharpCodeBuilder.StringLiteral(constraint.Name)});");
                foreach (var id in constraint.ReleasedBy)
                {
                    builder.Line($"builder.RemovedWith<{TypeName(context.Events[id], context)}>();");
                }
            }
        }

        builder.EndBlock().EndBlock();
        return new(Path.Combine([.. SliceNaming.FolderPath(location.Path), $"{className}.cs"]), builder.ToString())
        {
            // Constraints have no SemanticId in ESM v1; attribute the artifact to its owning slice and target events.
            Sources = [slice.Id, .. constraint.Targets.Select(_ => _.EventContract)]
        };
    }

    static string TypeName(SemanticEventContract @event, SemanticApplicationContext context) =>
        $"global::{SliceNaming.Namespace(context.RootNamespace, context.DeclaringSlice(@event.Id).Path)}.{Identifiers.ToPascalCase(@event.Name)}";
}
