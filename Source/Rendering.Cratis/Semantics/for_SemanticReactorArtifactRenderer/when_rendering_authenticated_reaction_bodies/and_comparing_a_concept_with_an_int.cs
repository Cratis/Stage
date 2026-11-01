// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticReactorArtifactRenderer.when_rendering_authenticated_reaction_bodies.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticReactorArtifactRenderer.when_rendering_authenticated_reaction_bodies;

public class and_comparing_a_concept_with_an_int : a_rendered_reaction_body
{
    const string Body = "return @event.Amount > 1000 ? new object[] { new TicketOpened(Title, @event.Amount) } : null;";
    const string Model = """
        concept Money : Decimal
        module Support
          feature Tickets
            slice Automation Notify
              event TicketOpened
                title String
                amount Money
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
