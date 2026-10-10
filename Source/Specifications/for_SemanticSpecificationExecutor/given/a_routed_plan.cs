// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Specifications.Semantic;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.given;

public class a_routed_plan : Specification
{
    protected const string Source = """
        eventsource Account
          id "stored-account"
          identifier String
          stream Transactions
            id "stored-transactions"
            streamId String
        module Banking
          feature Deposits
            slice StateChange Deposit
              command Deposit
                accountId String identifier
                period String
                stream Account.Transactions
                  streamId = period
                produces Deposited
                  for accountId
              event Deposited
              event Historical
              specification Depositing
                when Deposit
                  accountId = "acc-1"
                  period = "2026-10"
                then Deposited
                  stream Account.Transactions
                    streamId = "2026-10"
        """;

    protected SemanticExecutionPlan _plan = null!;
    protected SemanticSpecification _specification = null!;
    protected SemanticCommand _command = null!;
    protected SemanticSpecificationRunRecord _result = null!;
    protected SemanticSpecificationRun _reference = null!;
    protected static SemanticTypeReference Text => SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text);

    void Establish()
    {
        _plan = Compile(Source);
        _specification = _plan.Specifications.Values.Single();
        _command = _plan.Commands.Values.Single();
    }

    protected static SemanticExecutionPlan Compile(string source)
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Routed"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("model"), "model", "model.play", source);
        var compilation = new SemanticModelCompiler().Compile("Routed", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join("; ", compilation.Diagnostics.Select(diagnostic => $"{diagnostic.Code}: {diagnostic.Message}")));
        var compiled = SemanticExecutionPlan.Compile(compilation.Value!.Model);
        Assert.True(compiled.Success, string.Join("; ", compiled.Issues));
        return compiled.Plan!;
    }

    protected SemanticExecutionPlan With(SemanticSpecification specification, SemanticCommand? command = null, SemanticEventStream? stream = null, bool legacy = false)
    {
        var model = _plan.Model;
        var module = model.Application.Modules.Single();
        var feature = module.Features.Single();
        var slice = feature.Slices.Single();
        var sources = model.Application.EventSources;
        if (stream is not null) sources = [sources[0] with { Streams = [stream] }];
        var application = model.Application with
        {
            EventSources = legacy ? [] : sources,
            Policies = legacy ? [new("NotGuest", new SemanticNotPolicyCondition(new SemanticRoleCondition("Guest")))] : model.Application.Policies,
            Modules = [module with { Features = [feature with { Slices = [slice with { Commands = [command ?? _command], Specifications = [specification] }] }] }]
        };
        return SemanticExecutionPlan.Compile(ExecutableSemanticModel.Create(legacy ? LanguageVersion.V7 : LanguageVersion.V8, legacy ? SemanticVersion.V7 : SemanticVersion.V8, application)).Plan!;
    }

    protected async Task Run(SemanticExecutionPlan plan)
    {
        var specification = plan.Specifications.Values.Single();
        _reference = new SemanticSpecificationRunner().Run(plan, specification.Id);
        var report = await new SemanticSpecificationExecutor().Run(plan, new([specification.Id]), new());
        _result = report.Results.Single();
    }

    protected void AssertParity(bool passed)
    {
        _reference.Passed.ShouldEqual(passed);
        Assert.True(_result.Outcome == (passed ? SemanticSpecificationOutcome.Passed : SemanticSpecificationOutcome.Failed), $"Stage: {_result.Outcome}; {string.Join(';', _result.Failures)}; {_result.Unsupported?.Details}");
    }
}
