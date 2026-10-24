// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticAutomationArtifactRenderer;

public class when_preserving_state_change_bytes : given.an_automation
{
    const string Expected = """
        using Cratis.Arc.Authorization;
        using Cratis.Arc.Commands.ModelBound;
        using Cratis.Arc.Validation;
        using Cratis.Chronicle.Events;

        namespace Reactions.Work.Processing.Start;

        [global::Cratis.Arc.Commands.ModelBound.CommandAttribute]
        [global::Cratis.Arc.Authorization.AllowAnonymousAttribute]
        public record StartWork(string Id, decimal Amount) : global::Cratis.Chronicle.Events.ICanProvideEventSourceId
        {
            /// <inheritdoc/>
            public global::Cratis.Chronicle.Events.EventSourceId GetEventSourceId() => Id;

            public global::Reactions.Work.Processing.Start.Started Handle() => new(Id, Amount);
        }

        /// <summary>
        /// The event that occurs when started.
        /// </summary>
        [global::Cratis.Chronicle.Events.EventTypeAttribute]
        public record Started(string Id, decimal Amount);

        """;
    string _source = string.Empty;

    void Because() => _source = SemanticStateChangeArtifactRenderer.Render(_context.SelectedSlices().Single(slice => slice.Slice.Name == "Start"), _context).Content;

    [Fact] void should_keep_the_pre_composition_source_bytes() => _source.ShouldEqual(Expected + Environment.NewLine);
}
