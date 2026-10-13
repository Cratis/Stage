// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Host.for_SemanticRuntime.given;

namespace Cratis.Stage.Host.for_SemanticChronicleRegistration.given;

public class a_stored_event_name : a_compliance_store
{
    void Establish() => _plan = compiled_plan.From("""
        module Records
          feature Recording
            slice StateChange Record
              command Record
                id String identifier
                produces Renamed
                  for id
              event Renamed
                id "Original"
        """);
}
