// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticReactorArtifactRenderer.when_rendering_authenticated_reaction_bodies.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticReactorArtifactRenderer.when_rendering_authenticated_reaction_bodies;

public class and_returning_an_event_declared_beside_the_reaction_for_a_trigger_in_another_slice : a_rendered_reaction_body
{
    const string Body = "return new object[] { new Notified(Title) };";
    const string Model = """
        module Support
          feature Tickets
            slice Automation Open
              event TicketOpened
                title String
            slice Automation Notify
              event Notified
                title String
              reaction Notifier
                when TicketOpened
                  ```csharp
                  RETURN
                  ```
        """;

    void Because() => Render(Model.Replace("RETURN", Body, StringComparison.Ordinal));

    [Fact] void should_publish_and_compile() => ShouldPublishAndCompile(Body);
}
#endif
