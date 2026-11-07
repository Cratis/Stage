// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Host.for_SemanticSpecificationRuns.given;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticSpecificationRuns;

public class when_running_a_model_with_graph_specifications : a_specification_endpoint
{
    protected override string Source => """
        concept PersonId : Uuid
        concept GroupId : Uuid

        module Directory
          feature People
            slice StateChange JoinGroup
              command JoinGroup
                personId PersonId identifier
                groupId  GroupId
                produces PersonJoined
                  for personId
                  groupId = groupId

              event PersonJoined
                groupId GroupId

              graph Membership
                node Person
                  personId PersonId key
                node Group
                  groupId GroupId key
                edge MEMBER_OF source Person target Group

              projection Membership => graph Membership
                node Person
                  from PersonJoined key $eventSourceId
                node Group
                  from PersonJoined key groupId
                edge MEMBER_OF
                  from PersonJoined source $eventSourceId target groupId

              specification JoiningDrawsTheEdge
                given PersonJoined
                  for "11111111-1111-4111-8111-111111111111"
                  groupId = "22222222-2222-4222-8222-222222222222"
                then graph Membership
                  has edge MEMBER_OF from "11111111-1111-4111-8111-111111111111" to "22222222-2222-4222-8222-222222222222"
        """;

    async Task Because() => await Request("{\"scopes\":[]}");

    [Fact] void should_return_ok() => _status.ShouldEqual(StatusCodes.Status200OK);
    [Fact] void should_report_the_graph_specification() => Report.Results.Count(record => record.SliceKind == "Graph").ShouldEqual(1);
    [Fact] void should_pass_the_graph_specification() => Report.Results.Single(record => record.SliceKind == "Graph").Outcome.ShouldEqual(SemanticSpecificationOutcome.Passed);
}
