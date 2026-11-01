// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticAutomationArtifactRenderer;

public class when_preserving_state_change_bytes : given.an_automation
{
    const string Expected = """
        namespace Reactions.Work.Processing.Start;

        [Command]
        [AllowAnonymous]
        public record StartWork(string Id, decimal Amount) : ICanProvideEventSourceId
        {
            /// <inheritdoc/>
            public EventSourceId GetEventSourceId() => Id;

            public Started Handle() => new(Id, Amount);
        }

        /// <summary>
        /// The event that occurs when started.
        /// </summary>
        [EventType]
        public record Started(string Id, decimal Amount);

        """;
    string _source = string.Empty;

    void Because() => _source = SemanticStateChangeArtifactRenderer.Render(_context.SelectedSlices().Single(slice => slice.Slice.Name == "Start"), _context).Content;

    [Fact] void should_keep_the_pre_composition_source_bytes() => _source.ShouldEqual(Expected + Environment.NewLine);
}
