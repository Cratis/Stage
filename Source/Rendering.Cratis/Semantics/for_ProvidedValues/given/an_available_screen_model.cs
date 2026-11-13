// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Cratis.Stage.Rendering.Cratis.Semantics.for_ProvidedValues.given;

public static class an_available_screen_model
{
    public static string Source => an_available_fleet_model.Source +
        "\n      screen VesselList\n        available for Plus\n      screen InheritedList\n\n  feature Public\n    slice StateView PublicList\n      screen PublicList\n";
}
#endif
