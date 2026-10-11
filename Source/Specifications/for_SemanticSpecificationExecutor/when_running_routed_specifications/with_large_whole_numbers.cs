// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.given;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_running_routed_specifications;

public class with_large_whole_numbers : a_routed_plan
{
    new const string Source = """
        concept Count : Int
        eventsource Stock
          identifier String
          stream Entries
            streamId Count
        module Inventory
          feature Stock
            slice StateChange Record
              command RecordStock
                id String identifier
                count Count
                stream Stock.Entries
                  streamId = count
                produces StockRecorded
                  for id
                  count = count
              event StockRecorded
                count Count
              specification RecordingPositive
                when RecordStock
                  id = "positive"
                  count = 900719925474099
                then StockRecorded
                  stream Stock.Entries
                    streamId = 900719925474099
                  count = 900719925474099
              specification RecordingNegative
                when RecordStock
                  id = "negative"
                  count = -900719925474099
                then StockRecorded
                  stream Stock.Entries
                    streamId = -900719925474099
                  count = -900719925474099
        """;

    SemanticSpecificationRunReport _report = null!;

    async Task Because() => _report = await new SemanticSpecificationExecutor().Run(Compile(Source), new([]), new());

    [Fact] void should_run_both_specifications() => _report.Results.Count.ShouldEqual(2);
    [Fact] void should_pass_both_specifications() => _report.Results.Select(result => result.Outcome).Distinct().ShouldContainOnly(SemanticSpecificationOutcome.Passed);
}
