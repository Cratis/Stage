// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Xml.Linq;
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Specifications;
using Cratis.Stage.Conformance.Specs.for_ThreeWayConformance.given;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Conformance.Specs.for_ThreeWayConformance;

// #557 compares nine invocation cases with the frozen vector expectations, not all sixteen prepared-decision vectors.
// Excluded: present-state-race-recorded/removal, no-fact-command-conflict, merged-keys' conflict branches,
// repeat-commit-*, reconstructed-nonprefix-history. Arc exposes Execute, not a replayable prepare/commit API.
public class when_comparing_protected_read_race_invocations(when_comparing_protected_read_race_invocations.context fixture) : IClassFixture<when_comparing_protected_read_race_invocations.context>
{
    [Fact] void should_execute_every_selected_invocation() => fixture.Executed.ShouldEqual(protected_read_race_probes.InvocationVectors.Length);

    public class context : a_generated_application
    {
        public int Executed { get; private set; }

        protected override ArtifactRenderPlan CreatePlan()
        {
            var model = SemanticModelSerializer.Deserialize(ProtectedCommandReadRaceVectors.Model.EsmBytes.AsSpan());

            return CratisRendering.Plan(model, SemanticExecutionPlan.Compile(model).Plan!, new(ArtifactRenderScopeKind.Application, model.Application.Id), new("ProtectedCommandReadRaces", "ProtectedCommandReadRaces"));
        }

        async Task Because()
        {
            foreach (var (name, source) in protected_read_race_probes.Files())
            {
                AddGeneratedSpecification(name, source);
            }

            BuildWarnings(await Run("race-debug-build.log", "build", "ProtectedCommandReadRaces.csproj", "-c", "Debug", "-warnaserror", "--nologo", "-m:3")).ShouldEqual(string.Empty);
            await Run("race-debug-test.log", "test", "ProtectedCommandReadRaces.csproj", "-c", "Debug", "--no-build", "--no-restore", "--nologo", "--logger", "trx;LogFileName=races.trx", "--results-directory", _evidence.FullName);
            XNamespace ns = "http://microsoft.com/schemas/VisualStudio/TeamTest/2010";
            var results = XDocument.Load(Path.Combine(_evidence.FullName, "races.trx")).Descendants(ns + "UnitTestResult").ToArray();
            foreach (var name in protected_read_race_probes.InvocationVectors)
            {
                var facts = results.Where(result => result.Attribute("testName")!.Value.Contains($".when_{name.Replace('-', '_')}.", StringComparison.Ordinal)).ToArray();
                facts.ShouldNotBeEmpty();
                facts.ShouldEachConformTo(fact => fact.Attribute("outcome")!.Value == "Passed");
                Executed++;
            }
        }
    }
}
#endif
