// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Host.for_SemanticRuntime.given;

namespace Cratis.Stage.Host.for_SemanticWorldRebuilder.given;

public class a_pinned_event : a_rebuildable_world
{
    void Establish()
    {
        _plan = compiled_plan.From(Source.Replace("ProjectRegistered", "Renamed", StringComparison.Ordinal)
            .Replace("event Renamed\n", "event Renamed\n        id \"Original\"\n", StringComparison.Ordinal));
        _event.Context.EventType.Id = "Original";
    }
}
