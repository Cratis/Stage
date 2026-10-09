// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_refusing_event_source_routes : Specification
{
    internal const string Source = """
        concept AccountId : Uuid
        concept Month : Int
        eventsource Account
          identifier AccountId
          stream Transactions
            streamId Month
        module Banking
          feature Deposits
            slice StateChange Deposit
              command Deposit
                accountId AccountId identifier
                month Month
                amount Decimal
                stream Account.Transactions
                  streamId = month
                produces event Deposited
                  for accountId
                  amount Decimal = amount
        """;

    ExecutableSemanticModel _model = null!;
    ArtifactRenderPlan _plan = null!;

    void Establish()
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Banking"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("routes"), "routes", "Routes.play", Source);
        var compilation = new SemanticModelCompiler().Compile("Banking", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join(Environment.NewLine, compilation.Diagnostics));
        _model = compilation.Value!.Model;
    }

    void Because() => _plan = CratisRendering.Plan(
        _model,
        SemanticExecutionPlan.Compile(_model).Plan!,
        new(ArtifactRenderScopeKind.Application, _model.Application.Id),
        new("Banking", "Banking"));

    [Fact] void should_compile_an_esm_v8_model() => _model.SemanticVersion.ShouldEqual(SemanticVersion.V8);
    [Fact] void should_retain_the_routed_command() => _model.Application.Modules.Single().Features.Single().Slices.Single().Commands.Single().Route.ShouldNotBeNull();
    [Fact] void should_refuse_the_unaudited_version() => _plan.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContainOnly(["STAGE-ESM-016"]);
    [Fact] void should_not_report_a_successful_plan() => _plan.Success.ShouldBeFalse();
    [Fact] void should_emit_no_partial_artifacts() => _plan.Artifacts.ShouldBeEmpty();
}
