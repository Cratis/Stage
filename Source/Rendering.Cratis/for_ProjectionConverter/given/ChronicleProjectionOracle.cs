// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Screenplay.Syntax.Projections;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_ProjectionConverter.given;

/// <summary>
/// Compiles the exact Chronicle kernel visitor and its concept dependencies against the released infrastructure.
/// No substitute visitor or copied semantics are used. The released 19.1.7 infrastructure may differ from the
/// checked-out kernel; this oracle tests the visitor's definition construction, not full kernel execution.
/// </summary>
internal static class ChronicleProjectionOracle
{
    static readonly Lazy<MethodInfo> _convert = new(Compile);

    // The kernel projection visitor's source closure, kept explicit so missing checkout files fail the gate.
    static readonly string[] _sources =
    [
        "Core/Projections/Engine/DefinitionLanguage/ProjectionDefinitionSyntaxVisitor.cs",
        "Core/Projections/Engine/DefinitionLanguage/UnsupportedProjectionSyntax.cs",
        "Concepts/WellKnownExpressions.cs",
        "Concepts/Projections/ProjectionOwner.cs",
        "Concepts/Projections/ProjectionId.cs",
        "Concepts/Projections/PropertyExpression.cs",
        "Concepts/Projections/Definitions/AutoMap.cs",
        "Concepts/Projections/Definitions/ChildrenDefinition.cs",
        "Concepts/Projections/Definitions/FromDefinition.cs",
        "Concepts/Projections/Definitions/FromDerivatives.cs",
        "Concepts/Projections/Definitions/FromEventPropertyDefinition.cs",
        "Concepts/Projections/Definitions/FromEveryDefinition.cs",
        "Concepts/Projections/Definitions/JoinDefinition.cs",
        "Concepts/Projections/Definitions/ProjectionDefinition.cs",
        "Concepts/Projections/Definitions/RemovedWithDefinition.cs",
        "Concepts/Projections/Definitions/RemovedWithJoinDefinition.cs",
        "Concepts/Events/EventType.cs",
        "Concepts/Events/EventTypeId.cs",
        "Concepts/Events/EventTypeGeneration.cs",
        "Concepts/EventSequences/EventSequenceId.cs",
        "Concepts/EventSequences/WellKnownEventSequences.cs",
        "Concepts/ReadModels/ReadModelIdentifier.cs",
        "Concepts/Observation/ObserverId.cs"
    ];

    public static object Visit(ProjectionSyntax syntax) => _convert.Value.Invoke(null, [syntax])!;

    static MethodInfo Compile()
    {
        var root = Environment.GetEnvironmentVariable("CHRONICLE_SOURCE_ROOT");
        if (string.IsNullOrWhiteSpace(root))
        {
            var stage = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../.."));
            root = Path.GetFullPath(Path.Combine(stage, "../Chronicle"));
        }

        var kernel = Path.Combine(root, "Source/Kernel");
        Assert.True(Directory.Exists(kernel), $"Chronicle source checkout missing: {kernel}");
        var files = _sources.Select(path => Path.Combine(kernel, path)).ToArray();
        Assert.True(files.Length >= 23 && files.All(File.Exists),
            $"Chronicle visitor closure is missing: {string.Join(", ", files.Where(path => !File.Exists(path)))}");
        var platform = (string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES");
        Assert.False(string.IsNullOrWhiteSpace(platform), "The .NET platform reference list is required for the Chronicle visitor oracle.");
        var referenceFiles = platform!.Split(Path.PathSeparator)
            .Where(path => !Path.GetFileName(path).StartsWith("Cratis.Chronicle.", StringComparison.Ordinal) &&
                !string.Equals(Path.GetFileName(path), "Cratis.Chronicle.dll", StringComparison.Ordinal))
            .Concat([
                typeof(ProjectionSyntax).Assembly.Location,
                Path.Combine(AppContext.BaseDirectory, "Cratis.Fundamentals.dll"),
                Path.Combine(AppContext.BaseDirectory, "Cratis.Chronicle.Infrastructure.dll")
            ])
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        Assert.All(referenceFiles, path => Assert.True(File.Exists(path), $"Oracle reference missing: {path}"));
        var references = referenceFiles.Select(path => MetadataReference.CreateFromFile(path)).ToArray();
        var syntaxTrees = files.Select(path => CSharpSyntaxTree.ParseText(File.ReadAllText(path), path: path))
            .Append(CSharpSyntaxTree.ParseText(
                "global using System; global using System.Collections.Generic; global using System.Linq; global using Cratis.Concepts; public static class ProjectionOracleEntry { public static object Visit(Cratis.Screenplay.Syntax.Projections.ProjectionSyntax syntax) => new Cratis.Chronicle.Projections.Engine.DeclarationLanguage.ProjectionDefinitionSyntaxVisitor(Cratis.Chronicle.Concepts.Projections.ProjectionOwner.Client).Visit(syntax); }"));
        var compilation = CSharpCompilation.Create(
            "ChronicleProjectionOracle",
            syntaxTrees,
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        using var stream = new MemoryStream();
        var result = compilation.Emit(stream);
        Assert.True(result.Success, string.Join(Environment.NewLine, result.Diagnostics.Where(_ => _.Severity == DiagnosticSeverity.Error)));
        return Assembly.Load(stream.ToArray()).GetType("ProjectionOracleEntry")!.GetMethod("Visit")!;
    }
}
