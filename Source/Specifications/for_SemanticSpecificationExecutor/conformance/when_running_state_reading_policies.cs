// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Specifications.Semantic;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.conformance;

// The scenario of Cratis/StudioIssues#1387 run by both engines: a command gated by the plan of the asset it acts on is denied for an asset whose plan
// lacks the feature, admitted for one whose plan has it, and denied for one with no plan recorded, because state that is not recorded never allows.
public class when_running_state_reading_policies : Specification
{
    const string Source = """
        policy OnTheExportPlan
          reads Plan as plan by assetId
          require authenticated and plan.exports == true
        module Fleet
          feature Assets
            slice StateChange ExportAsset
              command ExportAsset
                assetId String identifier
                authorize OnTheExportPlan
                produces AssetExported
                  for assetId
              event AssetExported
              specification WithoutThePlanIsDenied
                given caller
                  authenticated
                given readmodel Plan
                  assetId = "a1"
                  exports = false
                when ExportAsset
                  assetId = "a1"
                then denied
              specification OnThePlanIsAdmitted
                given caller
                  authenticated
                given readmodel Plan
                  assetId = "a1"
                  exports = true
                when ExportAsset
                  assetId = "a1"
                then AssetExported
                  for "a1"
              specification WithNoPlanRecordedIsDenied
                given caller
                  authenticated
                when ExportAsset
                  assetId = "a2"
                then denied
              specification AnotherAssetsPlanDoesNotCount
                given caller
                  authenticated
                given readmodel Plan
                  assetId = "a1"
                  exports = true
                when ExportAsset
                  assetId = "a2"
                then denied
            slice StateView Plans
              readmodel Plan
                assetId String key
                exports Bool
              event PlanSet
                exports Bool
              projection Build => Plan
                from PlanSet
                  assetId = $eventSourceId
                  exports = exports
        """;

    readonly List<string> _differences = [];
    int _executed;

    [Fact]
    public void should_run_every_specification() => _executed.ShouldEqual(4);

    [Fact]
    public void should_match_the_reference_for_every_plan() => Assert.True(_differences.Count == 0, string.Join(Environment.NewLine, _differences));

    protected async Task Because()
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Fleet"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("plans"), "plans", "Plans.play", Source);
        var compilation = new SemanticModelCompiler().Compile("Fleet", SemanticDocumentSet.Create([document], catalog));
        compilation.Success.ShouldBeTrue();
        var plan = SemanticExecutionPlan.Compile(compilation.Value!.Model).Plan!;
        foreach (var specification in plan.Specifications.Values)
        {
            var reference = new SemanticSpecificationRunner().Run(plan, specification.Id);
            var result = (await new SemanticSpecificationExecutor().Run(plan, new([specification.Id]), new())).Results.Single();
            _executed++;
            if (!reference.Passed || result.Outcome != SemanticSpecificationOutcome.Passed)
            {
                _differences.Add($"{specification.Name}: reference {reference.Execution.Kind}/{reference.Passed} {string.Join(';', reference.Failures)}; Stage {result.Outcome} {result.Unsupported?.Details} {string.Join(';', result.Failures)}");
            }
        }
    }
}
