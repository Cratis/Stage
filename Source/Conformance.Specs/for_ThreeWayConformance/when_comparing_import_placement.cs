// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Conformance.Specs.for_ThreeWayConformance.given;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Xunit;

namespace Cratis.Stage.Conformance.Specs.for_ThreeWayConformance;

public class when_comparing_import_placement : Specification
{
    RenderedFile[] _before = null!;
    RenderedFile[] _after = null!;
    QualifiedOutputResult _proof = null!;

    void Establish()
    {
        const string body = """
            public static class Authored
            {
                public static int Run()
                {
                    var values = new[] { 1, 2 }.ToImmutableArray();
                    foreach (var value in values) { if (value > 1) return value; }
                    return 0;
                }
            }
            """;
        _before = [new("Authored.cs", "namespace Fixture.Inner;\nusing System.Collections.Immutable;\n" + body)];
        _after = [new("Authored.cs", "namespace Fixture.Inner;\n" + body)];
    }

    void Because() => _proof = QualifiedOutputProof.VerifyRendered(_before, _after, legacyImports: true);

    [Fact] void should_compile_before_changing_the_imports() => Assert.Empty(QualifiedOutputProof.CompilationErrors(_before, legacyImports: true));
    [Fact] void should_compile_with_the_scaffold_globals() => Assert.Empty(QualifiedOutputProof.CompilationErrors(_after));
    [Fact] void should_preserve_authored_extension_and_pattern_bindings() => Assert.Empty(_proof.Failures);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void should_report_a_compiling_binding_change_from_the_scaffold_globals(bool pattern)
    {
        const string support = """
            namespace Fixture.Extensions
            {
                public static class ExistingExtensions
                {
                    public static int Pick(this object value) => 1;
                    public static global::System.Runtime.CompilerServices.TaskAwaiter<int> GetAwaiter(this object value) => global::System.Threading.Tasks.Task.FromResult(1).GetAwaiter();
                }
            }
            namespace Cratis.Chronicle.Keys
            {
                public static class AddedExtensions
                {
                    public static int Pick(this string value) => 2;
                    public static global::System.Runtime.CompilerServices.TaskAwaiter<int> GetAwaiter(this string value) => global::System.Threading.Tasks.Task.FromResult(2).GetAwaiter();
                }
            }
            """;
        var body = pattern
            ? "public static class Authored { public static async global::System.Threading.Tasks.Task<int> Run() => await \"text\"; }"
            : "public static class Authored { public static int Run() => \"text\".Pick(); }";
        RenderedFile[] files = [new("Support.cs", support), new("Authored.cs", "using Fixture.Extensions;\nnamespace Authored;\n" + body)];

        Assert.Empty(QualifiedOutputProof.CompilationErrors(files, legacyImports: true));
        Assert.Empty(QualifiedOutputProof.CompilationErrors(files));
        Assert.Contains(QualifiedOutputProof.VerifyRendered(files, files, legacyImports: true).Failures,
            failure => failure.Contains(pattern ? "GetAwaiter" : "Pick", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void should_report_a_compiling_authored_extension_rebinding(bool globalImport)
    {
        const string support = """
            namespace Fixture
            {
                public static class AncestorExtensions { public static int Pick(this string value) => 1; }
            }
            namespace Fixture.Extensions
            {
                public static class ImportedExtensions { public static int Pick(this object value) => 2; }
            }
            """;
        const string body = "public static class Authored { public static int Run() => \"text\".Pick(); }";
        RenderedFile[] before = [new("Support.cs", support),
            new("Authored.cs", "namespace Fixture.Inner;\nusing global::Fixture.Extensions;\n" + body)];
        RenderedFile[] after = [new("Support.cs", support), new("Authored.cs", globalImport
            ? "namespace Fixture.Inner;\n" + body
            : "using global::Fixture.Extensions;\nnamespace Fixture.Inner;\n" + body)];
        if (globalImport) after = [.. after, new("GlobalUsings.cs", "global using Fixture.Extensions;")];

        Assert.Empty(QualifiedOutputProof.CompilationErrors(before, legacyImports: true));
        Assert.Empty(QualifiedOutputProof.CompilationErrors(after));
        Assert.Contains(QualifiedOutputProof.VerifyRendered(before, after, legacyImports: true).Failures,
            failure => failure.Contains("Pick", StringComparison.Ordinal) && failure.Contains("bound to", StringComparison.Ordinal));
    }
}
#endif
