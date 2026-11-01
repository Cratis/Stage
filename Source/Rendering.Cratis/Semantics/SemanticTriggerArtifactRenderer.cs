// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Naming;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Renders the discovered plaintext payload of an admitted application trigger.
/// </summary>
internal static class SemanticTriggerArtifactRenderer
{
    internal static RenderedFile Render(LocatedSemanticSlice located, SemanticApplicationTrigger trigger, SemanticApplicationContext context)
    {
        var name = Identifiers.ToPascalCase(trigger.Name);
        var builder = context.CodeBuilder().Namespace(SliceNaming.Namespace(context.RootNamespace, located.Path));
        var types = new SemanticTypeSystem(context);
        context.Docs(trigger.Id).Render(builder)
            .Attribute($"global::Cratis.Chronicle.Occurrences.TriggerAttribute({CSharpCodeBuilder.StringLiteral(trigger.Id.ToString())})")
            .Line($"public record {name}({string.Join(", ", trigger.Properties.Select(property => $"{types.Type(property.Type)} {Identifiers.ToPascalCase(property.Name)}"))});");
        return new(Path.Combine([.. SliceNaming.FolderPath(located.Path), $"{name}.cs"]), builder.ToString()) { Sources = [trigger.Id] };
    }
}
