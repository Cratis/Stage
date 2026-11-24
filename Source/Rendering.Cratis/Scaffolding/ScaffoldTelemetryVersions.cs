// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Cratis.Scaffolding;

/// <summary>Selects the centrally pinned telemetry assembly for the emitted application's framework.</summary>
internal static class ScaffoldTelemetryVersions
{
    internal static string ForTargetFramework(string framework) => framework switch
    {
        "net10.0" => ScaffoldPackageVersions.TelemetryAbstractionsNet10,
        "net11.0" => ScaffoldPackageVersions.TelemetryAbstractionsNet11,
        _ => throw new InvalidOperationException($"Unsupported scaffold target framework '{framework}'.")
    };
}
