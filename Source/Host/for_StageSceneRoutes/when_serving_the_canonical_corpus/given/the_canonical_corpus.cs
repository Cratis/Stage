// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Scene.Model.Elements;
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Stage.Api;
using Cratis.Stage.Contracts;
using Cratis.Stage.Contracts.Scene;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Stage.Host.for_StageSceneRoutes.when_serving_the_canonical_corpus.given;

/// <summary>
/// The canonical screen-composition corpus, run by the Stage host against two work items with one comment each,
/// with its scene resolved against the endpoints the host actually mapped.
/// </summary>
public class the_canonical_corpus : for_StageEndpointMapper.given.a_routed_model
{
    protected const string WorkItemA = "3fa85f64-5717-4562-b3fc-2c963f66afa6";
    protected const string WorkItemB = "22222222-2222-2222-2222-222222222222";
    protected const string TitleA = "Implement native browser forms";
    protected const string TitleB = "Validate selected work item only";
    protected const string CommentForA = "Needs compact layout";
    protected const string CommentForB = "Check the B scope";

    protected SceneApplication _scene = null!;
    protected IReadOnlyDictionary<string, string> _queryRoutes = null!;
    protected IReadOnlyDictionary<string, string> _commandRoutes = null!;
    string _directory = null!;

    async Task Establish()
    {
        _directory = Path.Combine(Path.GetTempPath(), $"stage-canonical-corpus-{Guid.NewGuid():N}");
        var form = ScreenCompositionCorpus.V1.SourceForms.Single(_ => _.Name == "folder");
        foreach (var document in form.Documents)
        {
            var path = Path.Combine(_directory, document.DisplayPath);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            await File.WriteAllTextAsync(path, document.Text);
        }

        var application = await EventModelLoader.LoadStageApplicationFromPathAsync(_directory);
        MapModel(application.EventModel);

        var readModels = StageModelWalker.Slices(application.EventModel)
            .Where(located => located.Slice.ReadModel is not null)
            .ToDictionary(located => located.Slice.ReadModel!.Name, located => located.Slice.ReadModel!.Id.ToString(), StringComparer.Ordinal);
        _readModelDocuments[readModels["WorkItemSummary"]] =
        [
            JsonSerializer.Serialize(new { id = WorkItemA, workItemId = WorkItemA, title = TitleA, status = "open" }),
            JsonSerializer.Serialize(new { id = WorkItemB, workItemId = WorkItemB, title = TitleB, status = "open" })
        ];
        _readModelDocuments[readModels["CommentView"]] =
        [
            JsonSerializer.Serialize(new { id = "11111111-1111-1111-1111-111111111111", commentId = "11111111-1111-1111-1111-111111111111", workItemId = WorkItemA, text = CommentForA }),
            JsonSerializer.Serialize(new { id = "33333333-3333-3333-3333-333333333333", commentId = "33333333-3333-3333-3333-333333333333", workItemId = WorkItemB, text = CommentForB })
        ];

        var endpoints = new CompositeEndpointDataSource(((IEndpointRouteBuilder)_app).DataSources);
        _scene = StageSceneRoutes.WithRoutes(SceneSynthesizer.Synthesize(application.Scene, application.EventModel), endpoints, NullLogger.Instance);
        _queryRoutes = StageSceneRoutes.RoutesByName(endpoints, "GET");
        _commandRoutes = StageSceneRoutes.RoutesByName(endpoints, "POST");
    }

    void Destroy()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    /// <summary>
    /// Gets every component on a screen that reads the named query.
    /// </summary>
    /// <param name="screen">The screen name.</param>
    /// <param name="query">The query name.</param>
    /// <returns>The components.</returns>
    protected IReadOnlyList<ExternalComponent> Reading(string screen, string query) =>
        [.. All(_scene.Screens.Single(candidate => candidate.Name == screen).SlotContent.Values.SelectMany(elements => elements))
            .Where(component => component.Properties.TryGetValue("query", out var value) && (value as string) == query)];

    /// <summary>
    /// Gets the texts a query answered with.
    /// </summary>
    /// <param name="response">The response.</param>
    /// <param name="property">The property to read from each instance.</param>
    /// <returns>The texts.</returns>
    protected static IReadOnlyList<string> Values((int Status, string Body, string? EndpointName) response, string property)
    {
        // A query that found nothing answers without a data member at all rather than with a null one.
        using var document = JsonDocument.Parse(response.Body);
        if (!document.RootElement.TryGetProperty("data", out var data))
        {
            return [];
        }

        return data.ValueKind switch
        {
            JsonValueKind.Array => [.. data.EnumerateArray().Select(instance => instance.GetProperty(property).GetString()!)],
            JsonValueKind.Null => [],
            _ => [data.GetProperty(property).GetString()!]
        };
    }

    /// <summary>
    /// Gets every component on every screen, nested ones included.
    /// </summary>
    /// <returns>The components.</returns>
    protected IEnumerable<ExternalComponent> Everywhere() => All(_scene.Screens.SelectMany(screen => screen.SlotContent.Values.SelectMany(elements => elements)));

    static IEnumerable<ExternalComponent> All(IEnumerable<SceneElement> elements)
    {
        foreach (var component in elements.OfType<ExternalComponent>())
        {
            yield return component;
            foreach (var nested in All(component.Slots.Values.SelectMany(slot => slot)))
            {
                yield return nested;
            }
        }
    }
}
