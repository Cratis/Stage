// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Cratis.Stage.Rendering.Cratis.Semantics.for_ProvidedValues.given;

/// <summary>
/// The fleet of <see cref="a_fleet_model"/> with audiences, a reusable availability and gates at every level.
/// </summary>
internal static class an_available_fleet_model
{
    internal const string Source = """
        audience Plus
          description "Customers on the Plus package"

        audience Beta
          description "Tenants in the beta"

        availability DataQuality
          description "Sold with Plus once the region is known"
          for Plus
          when $context.region == "Nordics"

        module Fleet
          provide region String from "Nordics"
          provide limit Int from 5

          feature Vessels
            available with DataQuality

            slice StateChange RegisterVessel
              available for Beta
              command RegisterVessel
                available for Plus or Beta
                vesselId Uuid identifier
                name String
                produces VesselRegistered
                  for vesselId
                  name = name
              event VesselRegistered
                name String

            slice StateView Vessels
              query VesselByKey => Vessel optional
                available when $context.region == "Nordics" and $context.limit >= 3
                by vesselId Uuid

              readmodel Vessel
                vesselId Uuid key
                name String

              projection Vessels => Vessel
                from VesselRegistered
                  vesselId = $eventSourceId
                  name = name
        """;
}
#endif
