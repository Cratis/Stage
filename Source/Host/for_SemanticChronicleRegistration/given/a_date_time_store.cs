// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Contracts.EventTypes;
using Cratis.Stage.Host.for_SemanticRuntime.given;
using NSubstitute;

namespace Cratis.Stage.Host.for_SemanticChronicleRegistration.given;

public class a_date_time_store : a_seeding_store
{
    protected JsonNode _registered = null!;

    protected void PrepareDateTime()
    {
        Prepare(string.Empty);
        _plan = compiled_plan.From("""
            concept Instant : DateTime
            module Clock
              feature Ticks
                slice StateChange RecordTick
                  command RecordTick
                    id String identifier
                    at Instant
                    produces TickRecorded
                      for id
                      at = at
                  event TickRecorded
                    at Instant
            """);
        _contracts.EventTypes.RegisterEventTypes(Arg.Do<RegisterEventTypesRequest>(request =>
            _registered = JsonNode.Parse(request.Types.Single().Schema)!));
    }
}
