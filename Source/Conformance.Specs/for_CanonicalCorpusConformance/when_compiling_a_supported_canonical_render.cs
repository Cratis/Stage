// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Conformance.Specs.for_CanonicalCorpusConformance.given;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Conformance.Specs.for_CanonicalCorpusConformance;

public class when_compiling_a_supported_canonical_render : a_rendered_canonical_vector
{
    IReadOnlyList<compile_error> _errors = null!;
    string[] _specifications = null!;
    string[] _compiledPaths = null!;

    void Because()
    {
        _errors = rendered_compilation.Errors(_plan);
        _specifications = [.. _plan.Artifacts.Select(file => file.RelativePath).Where(path => path.EndsWith(".cs", StringComparison.Ordinal) && Path.GetFileName(path).StartsWith("when_", StringComparison.Ordinal))];
        _compiledPaths = [.. rendered_compilation.Compilation(_plan.Artifacts).SyntaxTrees.Select(tree => tree.FilePath)];
    }

    [Fact]
    void should_reject_a_render_without_a_project() =>
        Assert.ThrowsAny<Exception>(() => rendered_compilation.Errors(_plan.Artifacts.Where(file => !file.RelativePath.EndsWith(".csproj", StringComparison.Ordinal)))).Message.ShouldEqual("A published canonical render must contain exactly one C# project.");

    [Fact]
    void should_reject_multiple_projects() =>
        Assert.ThrowsAny<Exception>(() => rendered_compilation.Errors(_plan.Artifacts.Append(PlannedArtifact.CreateText("Other.csproj", "<Project />")))).Message.ShouldEqual("A published canonical render must contain exactly one C# project.");

    [Fact]
    void should_treat_errors_without_a_source_as_harness_errors() =>
        Assert.ThrowsAny<Exception>(() => rendered_compilation.Errors(_plan.Artifacts.Where(file => file.RelativePath != "Program.cs"))).Message.ShouldContain("CS5001");

    [Fact]
    void should_treat_synthetic_usings_errors_as_harness_errors() =>
        Assert.ThrowsAny<Exception>(() => rendered_compilation.Errors(_plan.Artifacts.Append(PlannedArtifact.CreateText("GlobalUsings.g.cs", "class Probe { MissingType _field; }")))).Message.ShouldContain("CS0246");

    [Fact] void should_exclude_the_test_host_kernel() => rendered_compilation.Compilation(_plan.Artifacts).References.Any(reference => Path.GetFileName(reference.Display) == "Cratis.Chronicle.Core.dll").ShouldBeFalse();
    [Fact] void should_have_no_errors() => _errors.ShouldBeEmpty();
    [Fact] void should_publish_six_debug_specifications() => _specifications.Length.ShouldEqual(6);
    [Fact] void should_compile_the_debug_specifications() => _specifications.Except(_compiledPaths, StringComparer.Ordinal).ShouldBeEmpty();
}
#endif
