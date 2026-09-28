// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Stage.Rendering.Cratis.Naming;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

/// <summary>
/// Keeps positional-record admission tied to Roslyn's actual synthesized and inherited members.
/// </summary>
public class when_compiling_generated_members
{
    static readonly MetadataReference[] References = [.. ((string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") ?? string.Empty)
        .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries).Select(path => MetadataReference.CreateFromFile(path))];

    [Theory]
    [InlineData("EqualityContract")]
    [InlineData("ToString")]
    [InlineData("Equals")]
    [InlineData("GetHashCode")]
    [InlineData("Deconstruct")]
    [InlineData("PrintMembers")]
    [InlineData("GetType")]
    [InlineData("MemberwiseClone")]
    [InlineData("Finalize")]
    [InlineData("ReferenceEquals")]
    [InlineData("Clone")]
    [InlineData("Wait")]
    public void should_track_the_compiler_for_inherited_and_synthesized_members(string member)
    {
        var generated = GeneratedPascalCase.From(member);
        var source = $"public record Sample(string {generated});";
        var compilation = CSharpCompilation.Create(
            "Probe",
            [CSharpSyntaxTree.ParseText(source)],
            References,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        var errors = compilation.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ToArray();
        var admitted = GeneratedPascalCase.EventMembersAreUnique("Sample", [member]);
        Assert.True(admitted == (errors.Length == 0), $"{member}: admitted={admitted}; {string.Join("; ", errors.Select(error => error.ToString()))}");
    }

    [Theory]
    [InlineData("ProjectById")]
    [InlineData("ToString")]
    [InlineData("GetType")]
    [InlineData("Equals")]
    [InlineData("Sample")]
    [InlineData("EqualityContract")]
    [InlineData("Deconstruct")]
    [InlineData("PrintMembers")]
    [InlineData("ReferenceEquals")]
    [InlineData("Clone")]
    public void should_track_the_compiler_for_generated_query_names(string query)
    {
        var generated = GeneratedPascalCase.From(query);
        var source = $"public record Sample(string Id) {{ public static string {generated}(string id) => id; }}";
        var compilation = CSharpCompilation.Create(
            "Probe",
            [CSharpSyntaxTree.ParseText(source)],
            References,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        var errors = compilation.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ToArray();
        var admitted = GeneratedPascalCase.ReadModelMembersAreUnique("Sample", ["Id"], [query]);
        Assert.True(admitted == (errors.Length == 0), $"{query}: admitted={admitted}; {string.Join("; ", errors.Select(error => error.ToString()))}");
    }
}
#endif
