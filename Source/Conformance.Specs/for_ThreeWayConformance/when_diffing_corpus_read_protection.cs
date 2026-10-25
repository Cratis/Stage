// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Text;
using System.Xml.Linq;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Conformance.Specs.for_CanonicalCorpusConformance.given;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.Naming;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_ThreeWayConformance;

public class when_diffing_corpus_read_protection : Specification
{
    readonly List<string> _previouslyProtected = [];
    readonly List<string> _projectProtected = [];

    void Because()
    {
        foreach (var vector in canonical_corpus_vectors.All) Compare(vector);
    }

    void Compare(conformance_vector vector)
    {
        // Golden application display names are not always scaffold identifiers; preserve all command identities.
        var model = ExecutableSemanticModel.Create(vector.Model.LanguageVersion, vector.Model.SemanticVersion, vector.Model.Application with { Name = "CorpusApp" });
        if (SemanticExecutionPlan.Compile(model, []).Plan is not { } execution) return;
        var plan = CratisRendering.Plan(model, execution, new(ArtifactRenderScopeKind.Application, model.Application.Id), new("CorpusApp", "CorpusApp"));
        if (vector.Name == "ProtectedCommandReadsCorpus.V25") Assert.True(plan.Success, string.Join('\n', plan.Diagnostics.Select(diagnostic => diagnostic.Message)));
        if (!plan.Success) return; // Refused models have no generated commands to compare.

        var code = string.Join('\n', plan.Artifacts.Where(artifact => artifact.RelativePath.EndsWith(".cs", StringComparison.Ordinal)).Select(artifact => Encoding.UTF8.GetString(artifact.Bytes.AsSpan())));
        Assert.DoesNotContain("ReadProtectionAttribute", code, StringComparison.Ordinal);
        Assert.DoesNotContain("UnprotectedAttribute", code, StringComparison.Ordinal);
        XDocument.Parse(Encoding.UTF8.GetString(plan.Artifacts.Single(artifact => artifact.RelativePath == "Directory.Build.props").Bytes.AsSpan()))
            .Descendants("CratisReadProtection").Single().Value.ShouldEqual("On");

        // Before #784 this exact predicate emitted ReadProtection(On) on each command.
        foreach (var command in SemanticVersionFeatures.Slices(model.Application).SelectMany(slice => slice.Commands).Where(command => !command.Reads.IsDefaultOrEmpty))
        {
            _previouslyProtected.Add($"{vector.Name}/{command.Id}");
            Assert.Contains($"public record {Identifiers.ToPascalCase(command.Name)}(", code, StringComparison.Ordinal);
            _projectProtected.Add($"{vector.Name}/{command.Id}");
        }
    }

    [Fact] void should_compare_commands_that_previously_had_protection() => _previouslyProtected.ShouldNotBeEmpty();
    [Fact] void should_include_all_three_canonical_protected_commands() => _previouslyProtected.Count(identity => identity.StartsWith("ProtectedCommandReadsCorpus.V25/", StringComparison.Ordinal)).ShouldEqual(3);
    [Fact] void should_keep_every_previously_protected_command_under_project_protection() => _projectProtected.ShouldEqual(_previouslyProtected);
}
#endif
