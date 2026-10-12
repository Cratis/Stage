// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticAutomationArtifactRenderer;

public class when_preserving_state_view_bytes : given.an_automation
{
    const string View = """
            slice StateView Summary
              readmodel Summary
                id String
                amount Decimal
              query ById => Summary?
                by id String
              projection SummaryProjection => Summary
                from Started key id
                  id = id
                  amount = amount
        """;
    string _source = string.Empty;
    string _expected = string.Empty;

    void Establish()
    {
        SetContext(Compile(Source + "\n" + View));
        var model = _context.ReadModels.Values.Single();
        var parameters = string.Join(", ", model.Properties.OrderBy(property => property.Id.ToString(), StringComparer.Ordinal)
            .Select(property => property.Name == "id" ? "[global::Cratis.Chronicle.Keys.KeyAttribute] string Id" : "decimal Amount"));
        _expected = $$"""
            // Copyright (c) Cratis. All rights reserved.
            // Licensed under the MIT license. See LICENSE file in the project root for full license information.

            using Cratis.Arc.Authorization;
            using Cratis.Arc.Queries.ModelBound;
            using Cratis.Chronicle.Events;
            using Cratis.Chronicle.Keys;
            using Cratis.Chronicle.Projections.ModelBound;
            using Cratis.Chronicle.ReadModels;
            using Reactions.Work.Processing.Start;

            namespace Reactions.Work.Processing.Summary;

            [global::Cratis.Chronicle.Projections.ModelBound.FromEventAttribute<global::Reactions.Work.Processing.Start.Started>]
            [global::Cratis.Arc.Queries.ModelBound.ReadModelAttribute]
            public record Summary({{parameters}})
            {

                [global::Cratis.Arc.Authorization.AllowAnonymousAttribute]
                public static async global::System.Threading.Tasks.Task<Summary?> ById(global::Cratis.Chronicle.ReadModels.IReadModels readModels, string id) => await readModels.GetInstanceById<Summary>((global::Cratis.Chronicle.Events.EventSourceId)id);
            }
            """ + Environment.NewLine;
    }

    void Because() => _source = SemanticStateViewArtifactRenderer.Render(_context.SelectedSlices().Single(slice => slice.Slice.Name == "Summary"), _context).Content;

    [Fact] void should_keep_the_pre_composition_source_bytes() => _source.ShouldEqual(_expected);
}
