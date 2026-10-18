// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Host.for_SemanticSpecificationRuns.given;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticSpecificationRuns;

public class when_running_composite_event_values : a_specification_endpoint
{
    protected override string Source => """
        type Details
          label String
          count Int
        module Facts
          feature Recording
            slice StateChange Record
              command RecordDetails
                id String identifier
                details Details
                produces DetailsRecorded
                  for id
                  details = details
              event DetailsRecorded
                details Details
              specification RecordedComposite
                given DetailsRecorded
                  for "before"
                  details = {"label":"previous","count":6}
                when RecordDetails
                  id = "one"
                  details = {"label":"nested","count":7}
                then DetailsRecorded
                  details = {"count":7,"label":"nested"}
        """;

    Task Because() => Request("{}");

    [Fact] void should_return_ok() => _status.ShouldEqual(StatusCodes.Status200OK);
    [Fact] void should_pass_in_the_real_child_executor() => Report.Results.Single().Outcome.ShouldEqual(SemanticSpecificationOutcome.Passed);
    [Fact] void should_report_named_composite_members() => JsonSerializer.Deserialize<JsonElement>(Report.Results.Single().Trace!.Facts.Single().Values.Single().Value).GetProperty("label").GetString().ShouldEqual("nested");
}
