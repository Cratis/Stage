// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;

public static class a_native_context_model
{
    public const string Source = """
        type SeenDetails
          seen DateTime
        module Home
          feature Ctx
            slice StateView ContextTotals
              event ContextRecorded
                name String
              readmodel FromTotal
                id String key
                occurred DateTime
                correlation Uuid optional
                sequence Int
                source String
                details SeenDetails
              readmodel EveryTotal
                id String key
                occurred DateTime
                correlation Uuid optional
                sequence Int
                source String
              readmodel AllTotal
                id String key
                occurred DateTime
              projection FromProjection => FromTotal
                from ContextRecorded
                  id = $eventSourceId
                  occurred = $eventContext.occurred
                  correlation = $eventContext.correlationId
                  sequence = $eventContext.sequenceNumber
                  source = $eventContext.eventSourceId
                  details.seen = $eventContext.occurred
              projection EveryProjection => EveryTotal
                from ContextRecorded
                  id = $eventSourceId
                every
                  exclude children
                  occurred = $eventContext.occurred
                  correlation = $eventContext.correlationId
                  sequence = $eventContext.sequenceNumber
                  source = $eventContext.eventSourceId
              projection AllProjection => AllTotal
                from ContextRecorded
                  id = $eventSourceId
                all
                  occurred = $eventContext.occurred
              query FromTotals => FromTotal[]
              query EveryTotals => EveryTotal[]
              query AllTotals => AllTotal[]
        """;
}
