// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Text;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticReadModelBrowser.given;
using NSubstitute;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticReadModelBrowser.when_browsing;

public class with_a_command_for_an_instance_that_was_never_created : a_browser
{
    const string Route = "/api/tracking/tasks/overview/complete-task";
    HttpStatusCode _status;
    string _body = string.Empty;

    protected override string ModelSource => """
        module Tracking
          feature Tasks
            slice StateView Overview
              event TaskCreated
                projectId String
              event TaskCompleted
              command CreateTask
                id String identifier
                projectId String
                produces TaskCreated
                  for id
                  projectId = projectId
              command CompleteTask
                id String identifier
                produces TaskCompleted
                  for id
              readmodel TaskOverview
                id String key
                projectId String
              projection TaskOverviewProjection => TaskOverview
                from TaskCreated key $eventSourceId
                  id = $eventSourceId
                  projectId = projectId
                from TaskCompleted key $eventSourceId
        """;

    async Task Because()
    {
        using var response = await _client.PostAsync(Route, new StringContent("""{"id":"never-created"}""", Encoding.UTF8, "application/json"));
        _status = response.StatusCode;
        _body = await response.Content.ReadAsStringAsync();
    }

    [Fact] void should_answer_bad_request() => _status.ShouldEqual(HttpStatusCode.BadRequest);
    [Fact] void should_say_the_instance_does_not_exist() => _body.ShouldContain("The instance does not exist.");
    [Fact] void should_not_leak_the_internal_exception_text() => _body.ShouldNotContain("did not establish");
    [Fact] void should_not_append_an_event() => _facts.DidNotReceive().Append(Arg.Any<IReadOnlyList<SemanticFact>>(), Arg.Any<SemanticCommandOccurrence>());
}
