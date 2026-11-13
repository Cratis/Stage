// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Naming;
using Cratis.Stage.Rendering.Cratis.Scaffolding;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Admits and renders the audiences, availability blocks and <c language="text">available</c> gates of a model (Cratis/Stage#1117). The generated
/// application registers the declared audiences, one Choreography availability catalog and the gates of every module, feature, slice,
/// command and read model through Arc's availability hook, which evaluates them through the Choreography engine before
/// authorization and refuses a closed gate with the <c language="text">NotAvailable</c> reason. The audiences come from the Showrunner GuestList.
/// </summary>
internal static partial class SemanticAvailabilityRendering
{
    /// <summary>The diagnostic for a gate condition the Choreography engine cannot evaluate.</summary>
    internal const string UnsupportedCondition = "STAGE-ESM-077";

    /// <summary>The diagnostic for read model queries that need different gates.</summary>
    internal const string MixedQueryGates = "STAGE-ESM-078";

    const string Engine = "global::Cratis.Choreography.Availability";

    /// <summary>
    /// Determines whether the model declares an audience, an availability or a gate.
    /// </summary>
    /// <param name="context">The semantic application.</param>
    /// <returns><see langword="true"/> when the model uses availability.</returns>
    internal static bool Uses(SemanticApplicationContext context) =>
        !context.Application.Audiences.IsDefaultOrEmpty || !context.Application.Availabilities.IsDefaultOrEmpty || Gates(context).Count != 0;

    /// <summary>
    /// Refuses every availability shape the generated gates cannot evaluate faithfully.
    /// </summary>
    /// <param name="context">The semantic application.</param>
    /// <param name="diagnostics">The blocking diagnostics.</param>
    internal static void Admit(SemanticApplicationContext context, List<ArtifactRenderDiagnostic> diagnostics)
    {
        foreach (var availability in Blocks(context))
        {
            foreach (var requirement in availability.Requirements)
            {
                Refusal(requirement, $"Availability '{availability.Name}'", context, diagnostics);
            }
        }

        foreach (var gate in Gates(context))
        {
            foreach (var requirement in gate.Requirements)
            {
                Refusal(requirement, $"The gate on {gate.Description}", context, diagnostics);
            }
        }

        foreach (var readModel in context.AllSlices.SelectMany(located => located.Slice.Queries).GroupBy(query => query.ReadModel))
        {
            var signatures = readModel.Select(query => Signature(query.Available)).Distinct(StringComparer.Ordinal).ToArray();
            if (signatures.Length > 1)
            {
                diagnostics.Add(Error(MixedQueryGates, $"The queries of read model '{context.ReadModels[readModel.Key].Name}' need different availability gates; Arc gates the read model as a whole, so give every query of it the same gate.", context));
            }
        }
    }

    /// <summary>
    /// Renders the selection-independent registration and the package references it needs, or nothing when the model does not use availability.
    /// </summary>
    /// <param name="context">The semantic application.</param>
    /// <returns>The generated files.</returns>
    internal static IReadOnlyList<RenderedFile> Render(SemanticApplicationContext context)
    {
        if (!Uses(context)) return [];
        return [new(Path.Combine("GeneratedAvailability", "Availability.cs"), RenderRegistration(context)), new(Path.Combine("GeneratedAvailability", "Availability.props"), Props())];
    }

    static string Props() =>
        $"""
        <Project>
          <ItemGroup>
            <PackageReference Include="Cratis.Arc.Availability" VersionOverride="{ScaffoldPackageVersions.ArcAvailability}" />
            <PackageReference Include="Cratis.Showrunner.GuestList" VersionOverride="{ScaffoldPackageVersions.GuestList}" />
            <PackageReference Include="Cratis.Showrunner.GuestList.Availability" VersionOverride="{ScaffoldPackageVersions.GuestListAvailability}" />
          </ItemGroup>
        </Project>

        """;

    static string RenderRegistration(SemanticApplicationContext context)
    {
        var audiences = (context.Application.Audiences.IsDefault ? [] : context.Application.Audiences).Select(audience => audience.Name).Order(StringComparer.Ordinal).ToArray();
        var provides = SemanticProvidedValues.Declared(context).Count != 0;
        var builder = new CSharpCodeBuilder().Namespace($"{context.RootNamespace}.GeneratedAvailability")
            .Summary("Registers the audiences, availability blocks and gates of the Screenplay application.")
            .OpenBlock("public static partial class Registration")
            .Summary("The scope of each slice, outermost first, so a gate and a provided value are read where they are declared.")
            .Line($"static readonly global::System.Collections.Generic.Dictionary<string, {Engine}.Context.ContextScope> Scopes = new()")
            .Line("{");
        foreach (var scope in Scopes(context))
        {
            builder.Line($"    [{CSharpCodeBuilder.StringLiteral(scope.Namespace)}] = {Engine}.Context.ContextScope.Of({string.Join(", ", scope.Names.Select(CSharpCodeBuilder.StringLiteral))}),");
        }

        builder.Line("};").BlankLine()
            .OpenBlock("static partial void RegisterGenerated(global::Microsoft.Extensions.Hosting.IHostApplicationBuilder builder)")
            .Line("var services = builder.Services;")
            .Line($"var declared = new {Engine}.Audiences.DeclaredAudiences({string.Join(", ", audiences.Select(CSharpCodeBuilder.StringLiteral))});")
            .Line($"var catalog = new {Engine}.AvailabilityCatalog();");
        foreach (var availability in Blocks(context).OrderBy(block => block.Name, StringComparer.Ordinal))
        {
            builder.Line($"catalog.Add({Engine}.AvailabilityDefinition.FromLines({CSharpCodeBuilder.StringLiteral(availability.Name)}, {string.Join(", ", availability.Requirements.Select(Expression))}));");
        }

        builder.Line("catalog.Validate();")
            .Line($"global::Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddSingleton<{Engine}.Audiences.IDeclaredAudiences>(services, declared);")
            .Line("global::Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddSingleton(services, catalog);")
            .Line(provides
                ? $"global::Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddSingleton<{Engine}.Context.ContextValueCatalog>(services, provider => global::{context.RootNamespace}.GeneratedProvides.ProvidedValueResolvers.CreateCatalog(provider));"
                : $"global::Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddSingleton(services, new {Engine}.Context.ContextValueCatalog());")
            .BlankLine()
            .Line("// Every audience of a request comes from what the product recorded; the switch grants all declared audiences only in a development, local or test environment.")
            .Line("global::System.Action<" + Engine + ".AvailabilityOptions> engine = options =>")
            .Line("{")
            .Line("    options.EnvironmentName = builder.Environment.EnvironmentName;")
            .Line("    options.GrantAllDeclaredAudiences = global::Microsoft.Extensions.Configuration.ConfigurationBinder.GetValue<bool>(builder.Configuration, \"Availability:GrantAllDeclaredAudiences\");")
            .Line("};")
            .Line("global::Microsoft.Extensions.Hosting.GuestListHostApplicationBuilderExtensions.AddGuestList(builder, guestList =>")
            .Line("{")
            .Line($"    guestList.Product = {CSharpCodeBuilder.StringLiteral(Product(context))};")
            .Line($"    guestList.DeclaredAudiences = [{string.Join(", ", audiences.Select(CSharpCodeBuilder.StringLiteral))}];")
            .Line("});")
            .Line("global::Microsoft.Extensions.DependencyInjection.GuestListAvailabilityServiceCollectionExtensions.AddGuestListAvailability(services, engine);")
            .Line("global::Microsoft.Extensions.DependencyInjection.ArcAvailabilityServiceCollectionExtensions.AddArcAvailability(services, options =>")
            .Line("{")
            .Line($"    options.ScopeFor = type => Scopes.TryGetValue(type.Namespace ?? string.Empty, out var scope) ? scope : {Engine}.Context.ContextScope.Application;");
        foreach (var gate in Gates(context).OrderBy(gate => gate.Key, StringComparer.Ordinal))
        {
            var expression = GateExpression(gate);
            builder.Line(gate.IsType
                ? $"    GateType(options, {CSharpCodeBuilder.StringLiteral(gate.Key)}, {expression});"
                : $"    options.GateNamespace({CSharpCodeBuilder.StringLiteral(gate.Key)}, {expression});");
        }

        builder.Line("}, engine);")
            .BlankLine();
        AppendManifest(builder, context);
        builder
            .EndBlock().BlankLine()
            .Summary("Gates a command or read model by its full name, and fails loudly when the type is not part of the application instead of leaving it open.")
            .Line("/// <param name=\"options\">The Arc availability options.</param>")
            .Line("/// <param name=\"typeName\">The full name of the part.</param>")
            .Line("/// <param name=\"gate\">The gate.</param>")
            .OpenBlock($"static void GateType(global::Cratis.Arc.Availability.ArcAvailabilityOptions options, string typeName, {Engine}.AvailabilityExpression gate)")
            .Line("var type = typeof(Registration).Assembly.GetType(typeName) ?? throw new global::System.InvalidOperationException($\"The gated part '{typeName}' is not part of the application.\");")
            .Line("options.Gate(type, gate);")
            .EndBlock()
            .EndBlock();
        return builder.ToString();
    }

    static string Product(SemanticApplicationContext context) =>
        string.Concat(context.Application.Name.Where(char.IsLetterOrDigit)).ToLowerInvariant();

    static IReadOnlyList<(string Namespace, string[] Names)> Scopes(SemanticApplicationContext context) =>
        [.. context.AllSlices
            .Select(located => (Namespace: SliceNaming.Namespace(context.RootNamespace, located.Path), Names: (string[])[located.Module.Name, .. located.FeaturePath.Select(feature => feature.Name), located.Slice.Name]))
            .DistinctBy(scope => scope.Namespace)
            .OrderBy(scope => scope.Namespace, StringComparer.Ordinal)];

    static ImmutableArray<SemanticAvailability> Blocks(SemanticApplicationContext context) =>
        context.Application.Availabilities.IsDefault ? [] : context.Application.Availabilities;

    static string Signature(ImmutableArray<SemanticAvailabilityRequirement> gate)
    {
        if (gate.IsDefaultOrEmpty) return string.Empty;
        try
        {
            return string.Join('\n', gate.Select(Expression));
        }
        catch (UnsupportedSemanticRendering)
        {
            return "unsupported";
        }
    }

    static ArtifactRenderDiagnostic Error(string code, string message, SemanticApplicationContext context) =>
        new(code, ArtifactRenderDiagnosticSeverity.Error, message, context.Application.Id);
}
