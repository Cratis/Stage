// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Stage.Publishing;

/// <summary>
/// The version of this Stage runtime, without build metadata.
/// </summary>
internal static class StageRuntimeVersion
{
    internal static string Current { get; } = Read();

    static string Read()
    {
        var informational = typeof(StageRuntimeVersion).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        var version = informational ?? typeof(StageRuntimeVersion).Assembly.GetName().Version?.ToString(3) ?? "0.0.0";

        return version.Split('+')[0];
    }
}
