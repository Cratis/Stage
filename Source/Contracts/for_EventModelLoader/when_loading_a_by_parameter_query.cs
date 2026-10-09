// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Projections;
using Xunit;

namespace Cratis.Stage.Contracts.for_EventModelLoader;

public class when_loading_a_by_parameter_query : Specification
{
    const string Source =
        """
        concept WorkItemId : Uuid
        concept CommentId : Uuid
        module Workspaces
          feature Tracking
            slice StateChange AddComment
              command AddComment
                commentId CommentId identifier
                workItemId WorkItemId
                text String
                produces CommentAdded
                  for commentId
                  workItemId = workItemId
                  text = text
              event CommentAdded
                workItemId WorkItemId
                text String
            slice StateView WorkItemComments
              readmodel CommentView
                commentId CommentId
                workItemId WorkItemId
                text String
              query AllComments => CommentView[]
              query CommentsForWorkItem => observable CommentView[]
                by workItemId WorkItemId
              projection WorkItemComments => CommentView
                from CommentAdded
                  commentId = $eventSourceId
                  workItemId = workItemId
                  text = text
        """;

    ReadModelDefinition _readModel = null!;

    void Because()
    {
        var model = EventModelLoader.LoadFromSource(Source);
        _readModel = model.Collections.Single().Modules.Single().Features.Single().Slices.Single(slice => slice.Name == "WorkItemComments").ReadModel!;
    }

    [Fact] void should_carry_every_query_over_the_read_model() => _readModel.Queries.Select(query => query.Name).ShouldContainOnly(["AllComments", "CommentsForWorkItem"]);
    [Fact] void should_carry_the_by_parameter() => _readModel.Queries.Single(query => query.Name == "CommentsForWorkItem").Parameter.ShouldEqual("workItemId");
    [Fact] void should_leave_an_unkeyed_query_without_a_parameter() => _readModel.Queries.Single(query => query.Name == "AllComments").Parameter.ShouldBeNull();
    [Fact] void should_carry_the_collection_cardinality() => _readModel.Queries.Single(query => query.Name == "CommentsForWorkItem").IsCollection.ShouldBeTrue();
}
