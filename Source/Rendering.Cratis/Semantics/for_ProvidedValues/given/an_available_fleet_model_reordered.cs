// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Cratis.Stage.Rendering.Cratis.Semantics.for_ProvidedValues.given;

/// <summary>
/// The model of <see cref="an_available_fleet_model"/> with its declarations in another order: audiences swapped, the availability after the module, the slices swapped and the provided values swapped.
/// </summary>
internal static class an_available_fleet_model_reordered
{
    internal const string Source = """
        module Fleet
          provide limit Int from 5
          provide region String from "Nordics"

          feature Vessels
            available with DataQuality

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

        availability DataQuality
          description "Sold with Plus once the region is known"
          for Plus
          when $context.region == "Nordics"

        audience Beta
          description "Tenants in the beta"

        audience Plus
          description "Customers on the Plus package"
        """;
}
#endif
