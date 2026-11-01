// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Specifications.Semantic;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_running_reducer_observer_filters;

public class with_dynamic_fact_paths : Specification
{
    [Theory]
    [InlineData("reaction", false)]
    [InlineData("invocation", false)]
    [InlineData("refusal", false)]
    [InlineData("reaction", true)]
    [InlineData("invocation", true)]
    [InlineData("invocation-route", true)]
    [InlineData("refusal", true)]
    public async Task should_check_each_concrete_follow_up_before_append(string path, bool filtered)
    {
        var plan = Compile(path, filtered);
        var specification = plan.Specifications.Values.Single();
        var reference = new SemanticSpecificationRunner().Run(plan, specification.Id);
        var retained = new List<int>();
        var result = (await new SemanticSpecificationExecutor { ObserveWorldFacts = (_, count) => retained.Add(count) }.Run(plan, new([specification.Id]), new())).Results.Single();
        var refused = !filtered || path == "invocation-route";
        reference.Execution.Kind.ShouldEqual(refused ? SemanticExecutionOutcomeKind.Unsupported : SemanticExecutionOutcomeKind.Accepted);
        result.Outcome.ShouldEqual(refused ? SemanticSpecificationOutcome.Unsupported : SemanticSpecificationOutcome.Passed);
        if (refused)
        {
            result.Unsupported!.Details.ShouldContain("STAGE-IMPL-003:");
            retained.TrueForAll(count => count == (filtered ? 1 : 0)).ShouldBeTrue();
        }
        else
        {
            reference.Passed.ShouldBeTrue();
            result.Trace!.Facts.Count.ShouldEqual(2);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void should_refuse_an_authored_inbound_capture_with_a_local_reducer_input(bool filtered)
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("InvalidCaptureReducer"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("app"), "app", "app.play", CaptureSource(filtered));
        var compiled = new SemanticModelCompiler().Compile("InvalidCaptureReducer", SemanticDocumentSet.Create([document], catalog));
        compiled.Success.ShouldBeFalse();
        compiled.Diagnostics.Single(diagnostic => diagnostic.Code == "PLAY0616").Message.ShouldEqual("Explicitly inbound translation must consume foreign public events, not 'Changed'.");
    }

    static SemanticExecutionPlan Compile(string path, bool filtered)
    {
        var effect = path switch
        {
            "invocation" or "invocation-route" => "invokes Finish\n            id = \"one\"",
            "refusal" => "invokes Finish\n            id = \"one\"\n            on refused\n              produces Changed",
            _ => "produces Changed"
        };
        var source = $$"""
            eventsource Account
              stream Notes
            module Work
              feature Items
                slice Automation Manage
                  event Started
                  event Changed
                  command Feed
                    id String identifier
                    produces Started
                      for id
                  command Finish
                    id String identifier
            {{(path == "invocation-route" ? "        stream Account.Notes" : "")}}
            {{(path == "refusal" ? "        validate\n          require id != \"one\"\n            message \"Not now\"" : "        produces Changed\n          for id")}}
                  readmodel Snapshot
                    id String key
                  reducer Fold => Snapshot
            {{(filtered ? "        from Account.Notes" : "")}}
                    on Changed
                      ```csharp
                      throw new InvalidOperationException("Never execute an authored body");
                      ```
            {{"      reaction Follow\n        when Started\n          " + effect}}
                  specification Check
                    when Feed
                      id = "one"
                    then Started
                      for "one"
                    then Changed
                      for "one"
            """;
        return FromSource(source);
    }

    static string CaptureSource(bool filtered) => $$"""
        eventsource Account
          stream Notes
        module Work
          feature Items
            slice Translate Receive
              direction inbound
              event Started from "external"
                id String
              event Changed
                id String
              capture Feed
                source events
                  from Started
                key id
                append Changed
                  id = $.id
              readmodel Snapshot
                id String key
              reducer Fold => Snapshot
        {{(filtered ? "        from Account.Notes" : "")}}
                on Changed
                  ```csharp
                  throw new InvalidOperationException("Never execute an authored body");
                  ```
              specification Check
                when append Started
                  id = "one"
                then Changed
                  for "one"
                  id = "one"
        """;

    static SemanticExecutionPlan FromSource(string source)
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("DynamicFilters"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("app"), "app", "app.play", source);
        var compiled = new SemanticModelCompiler().Compile("Dynamic", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compiled.Success, string.Join(';', compiled.Diagnostics));
        return SemanticExecutionPlan.Compile(compiled.Value!.Model).Plan!;
    }
}
