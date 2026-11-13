// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Naming;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// The availability manifest of a model: what the model declares, as data the running application hands to its operator.
/// </summary>
internal static partial class SemanticAvailabilityRendering
{
    /// <summary>
    /// Renders the statements that build the manifest in the registration, every list sorted so the same model always renders the same bytes.
    /// A gate target reads <c language="text">kind:path;slices=N</c>, where the path joins the module, feature and slice names with a slash and N counts the slices at or beneath the target.
    /// </summary>
    /// <param name="builder">The registration being built.</param>
    /// <param name="context">The semantic application.</param>
    static void AppendManifest(CSharpCodeBuilder builder, SemanticApplicationContext context)
    {
        var audiences = (context.Application.Audiences.IsDefault ? [] : context.Application.Audiences).OrderBy(audience => audience.Name, StringComparer.Ordinal).ToArray();
        var blocks = Blocks(context).OrderBy(block => block.Name, StringComparer.Ordinal).ToArray();
        var gates = GateTargets(context).OrderBy(gate => gate.Target, StringComparer.Ordinal).ToArray();
        var values = SemanticProvidedValues.Declared(context)
            .OrderBy(declaration => string.Join('/', declaration.Scope), StringComparer.Ordinal)
            .ThenBy(declaration => declaration.Value.Name, StringComparer.Ordinal).ToArray();

        builder.Line($"var manifest = new {Engine}.AvailabilityManifest(")
            .Line($"    {CSharpCodeBuilder.StringLiteral(context.Application.Name)},")
            .Line("    [");
        foreach (var audience in audiences)
        {
            builder.Line($"        new {Engine}.AudienceDeclaration({CSharpCodeBuilder.StringLiteral(audience.Name)}),");
        }

        builder.Line("    ],").Line("    [");
        foreach (var block in blocks)
        {
            builder.Line($"        {Engine}.AvailabilityBlockEntry.From(catalog.Get({CSharpCodeBuilder.StringLiteral(block.Name)}), catalog),");
        }

        builder.Line("    ],").Line("    [");
        foreach (var gate in gates)
        {
            builder.Line($"        {Engine}.AvailabilityGateEntry.From({CSharpCodeBuilder.StringLiteral(gate.Target)}, {GateExpression(gate.Gate)}, catalog),");
        }

        builder.Line("    ],").Line("    [");
        foreach (var declaration in values)
        {
            var provided = declaration.Value;
            var required = !(provided.Optional || provided.Type?.IsOptional == true);
            var scope = declaration.Scope.Count == 0 ? $"{Engine}.Context.ContextScope.Application" : $"{Engine}.Context.ContextScope.Of({string.Join(", ", declaration.Scope.Select(CSharpCodeBuilder.StringLiteral))})";
            var type = provided.Type is null ? string.Empty : provided.Type.Primitive.ToString();
            builder.Line($"        new {Engine}.ContextValueEntry({CSharpCodeBuilder.StringLiteral(provided.Name)}, {CSharpCodeBuilder.StringLiteral(type)}, {CSharpCodeBuilder.StringLiteral(ProvidedOrigin(provided))}, {scope}, {(required ? "true" : "false")}),");
        }

        // The semantic model carries no descriptions, persona audiences or source positions (Screenplay authoring metadata), so none are invented.
        builder.Line("    ],").Line("    []);")
            .Line($"global::Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddSingleton<{Engine}.IAvailabilityManifest>(services, new {Engine}.InMemoryAvailabilityManifest(manifest));");
    }

    static string GateExpression(SemanticAvailabilityGate gate) =>
        gate.Requirements.Length == 1 ? Expression(gate.Requirements[0]) : $"{Engine}.AvailabilityExpression.AllOf({string.Join(", ", gate.Requirements.Select(Expression))})";

    static string ProvidedOrigin(SemanticProvidedValue provided) => provided.Source switch
    {
        SemanticHostProvideSource => "host",
        SemanticQueryProvideSource => "provided from a query",
        SemanticValueProvideSource => "provided from a value",
        _ => string.Empty
    };

    static int SliceCount(SemanticAvailabilityGate gate, string target, IEnumerable<string> sliceNamespaces)
    {
        // A screen marker is not a backend slice and must never hide the slice's command/query proxies.
        if (target.StartsWith("screen:", StringComparison.Ordinal)) return 0;
        return gate.IsType ? 1 : sliceNamespaces.Count(slice => slice == gate.Key || slice.StartsWith(gate.Key + ".", StringComparison.Ordinal));
    }

    static IEnumerable<(string Target, SemanticAvailabilityGate Gate)> GateTargets(SemanticApplicationContext context)
    {
        var sliceNamespaces = context.AllSlices.Select(located => SliceNaming.Namespace(context.RootNamespace, located.Path)).Distinct(StringComparer.Ordinal).ToArray();
        var paths = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var located in context.AllSlices)
        {
            var modulePrefix = located.Path.Count - located.FeaturePath.Count - 1;
            paths[SliceNaming.Namespace(context.RootNamespace, located.Path.Take(modulePrefix))] = $"module:{located.Module.Name}";
            var names = new List<string> { located.Module.Name };
            for (var index = 0; index < located.FeaturePath.Count; index++)
            {
                names.Add(located.FeaturePath[index].Name);
                paths[SliceNaming.Namespace(context.RootNamespace, located.Path.Take(modulePrefix + index + 1))] = $"feature:{string.Join('/', names)}";
            }

            var sliceNamespace = SliceNaming.Namespace(context.RootNamespace, located.Path);
            paths[sliceNamespace] = $"slice:{string.Join('/', names)}/{located.Slice.Name}";
            foreach (var command in located.Slice.Commands)
            {
                paths[$"{sliceNamespace}.{Identifiers.ToPascalCase(command.Name)}"] = $"command:{string.Join('/', names)}/{located.Slice.Name}/{command.Name}";
            }

            foreach (var query in located.Slice.Queries)
            {
                paths[$"{sliceNamespace}.{Identifiers.ToPascalCase(context.ReadModels[query.ReadModel].Name)}"] = $"query:{string.Join('/', names)}/{located.Slice.Name}/{query.Name}";
            }
        }

        SemanticScreenAvailability.AppendTargets(paths, context);

        foreach (var gate in Gates(context))
        {
            var beneath = SliceCount(gate, paths[gate.Key], sliceNamespaces);
            yield return ($"{paths[gate.Key]};slices={beneath}", gate);
        }
    }
}
