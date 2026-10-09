// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Stage.Contracts;
using Cratis.Stage.Contracts.Projections;
using Cratis.Stage.Host.for_StageHttpSurface.given;

namespace Cratis.Stage.Host.for_StageEndpointMapper.when_serving_a_by_parameter_collection_query.given;

public class a_comment_thread : for_StageEndpointMapper.given.a_routed_model
{
    protected const string ParentA = "3fa85f64-5717-4562-b3fc-2c963f66afa6";
    protected const string ParentB = "22222222-2222-2222-2222-222222222222";
    protected const string KeyedRoute = "/api/orders/checkout/work-item-comments/comments-for-work-item";
    protected const string LegacyKeyedRoute = "/api/orders/checkout/comments-for-work-item";
    protected const string CollectionRoute = "/api/orders/checkout/work-item-comments/all-comment-views";
    protected const string CommentForA = "Needs compact layout";
    protected const string CommentForB = "Validate selected work item only";

    protected (int Status, string Body, string? EndpointName) _response;

    void Establish()
    {
        var slice = RouteModels.Query("WorkItemComments", "CommentView");
        slice = slice with
        {
            ReadModel = slice.ReadModel! with
            {
                Schema = """{"type":"object","properties":{"commentId":{"type":"string"},"workItemId":{"type":"string"},"text":{"type":"string"}}}""",
                Queries = [new ReadModelQueryDefinition("CommentsForWorkItem", "workItemId", true)]
            }
        };

        _readModelDocuments[slice.ReadModel!.Id.ToString()] =
        [
            JsonSerializer.Serialize(new { id = "11111111-1111-1111-1111-111111111111", commentId = "11111111-1111-1111-1111-111111111111", workItemId = ParentA, text = CommentForA }),
            JsonSerializer.Serialize(new { id = "33333333-3333-3333-3333-333333333333", commentId = "33333333-3333-3333-3333-333333333333", workItemId = ParentB, text = CommentForB })
        ];

        MapModel(RouteModels.Model(slice));
    }

    protected IReadOnlyList<string> Texts()
    {
        using var document = JsonDocument.Parse(_response.Body);
        var data = document.RootElement.GetProperty("data");

        return [.. data.EnumerateArray().Select(instance => instance.GetProperty("text").GetString()!)];
    }
}
