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
                public static object Run(IMongoCollection<int> collection)
                {
                    var found = collection.Find(value => value > 1);
                    foreach (var value in new[] { 1, 2 }) { if (value > 1) return found; }
                    return found;
                }
            }
            """;
        _before = [new("Authored.cs", "namespace Fixture.Inner;\nusing MongoDB.Driver;\n" + body)];
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
}
#endif
