// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Naming;
using Cratis.Stage.Rendering.Cratis.Semantics.Validation;
using Microsoft.CodeAnalysis.CSharp;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>Checks the emitted synchronous reaction return contract against pinned net10 references.</summary>
internal static class SemanticReactionBodyAdmission
{
    internal static string? Failure(SemanticApplicationContext context, SemanticReaction reaction, InlineCode body)
    {
        var @event = context.Events[reaction.Triggers.Single().Source];
        var types = new SemanticTypeSystem(context);
        var eventNamespace = SliceNaming.Namespace(context.RootNamespace, context.DeclaringSlice(@event.Id).Path);
        var eventName = Identifiers.ToPascalCase(@event.Name);
        var properties = string.Join(", ", @event.Properties.Select(property => $"{types.Type(property.Type)} {Identifiers.ToPascalCase(property.Name)}"));
        var bindings = string.Join('\n', @event.Properties.Select(property => $"var {Identifiers.ToPascalCase(property.Name)} = @event.{Identifiers.ToPascalCase(property.Name)};"));
        var guard = reaction.Triggers[0].Where is { } condition ?
            $"if (!({SemanticConditionRendering.Render(condition, new(@event.Properties, "@event", context))})) return null;" : string.Empty;

        // The context is an opaque value here. Members absent from this explicitly represented shape fail closed;
        // admission does not invent Chronicle's runtime member contracts from their spelling.
        var source = SemanticBodyCompilation.ImplicitUsings + $"using {context.CommonNamespace}; using {eventNamespace};\n" +
            SemanticValidationContextRuntime.AnalysisDeclarations(context) +
            $"namespace {eventNamespace} {{ public record {eventName}({properties}); }}\n" +
            "namespace Cratis.Chronicle.Events { public sealed class EventContext { } }\n" +
            $"internal class ReactionProbe {{ public System.Collections.Generic.IEnumerable<object>? Handle({eventNamespace}.{eventName} @event, Cratis.Chronicle.Events.EventContext context) {{ {bindings}\n{guard}\n{body.Text}\n }} }}";
        var tree = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(Microsoft.CodeAnalysis.CSharp.LanguageVersion.Preview));
        var failures = SemanticBodyCompilation.Create("ReactionBodyProbe", tree).GetDiagnostics().Where(SemanticBodyCompilation.IsFailure).ToArray();
        return failures.Length == 0 ? null : string.Join("; ", failures.Select(diagnostic => diagnostic.GetMessage(System.Globalization.CultureInfo.InvariantCulture)));
    }
}
