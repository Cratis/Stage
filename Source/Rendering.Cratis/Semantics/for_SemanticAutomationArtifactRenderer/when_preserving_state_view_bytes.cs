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
            .Select(property => property.Name == "id" ? "[Key] string Id" : "decimal Amount"));
        _expected = $$"""
            using Reactions.Work.Processing.Start;

            namespace Reactions.Work.Processing.Summary;

            /// <summary>
            /// Represents summary.
            /// </summary>
            /// <param name="Amount">The amount.</param>
            /// <param name="Id">The id.</param>
            [FromEvent<Started>]
            [ReadModel]
            public record Summary({{parameters}})
            {

                /// <summary>
                /// Queries summary through ById.
                /// </summary>
                /// <param name="readModels">The read-model store.</param>
                /// <param name="id">The id to query.</param>
                /// <returns>The matching read-model result.</returns>
                public static async Task<Summary?> ById(IReadModels readModels, EventSourceId id) => await readModels.GetInstanceById<Summary>(id);
            }
            """ + Environment.NewLine;
    }

    void Because() => _source = SemanticStateViewArtifactRenderer.Render(_context.SelectedSlices().Single(slice => slice.Slice.Name == "Summary"), _context).Content;

    [Fact] void should_keep_the_pre_composition_source_bytes() => _source.ShouldEqual(_expected);
}
