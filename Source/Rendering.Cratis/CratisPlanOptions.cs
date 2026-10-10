// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Cratis;

/// <summary>
/// Defines application-level names and optional domain placement for a source plan.
/// </summary>
/// <param name="ApplicationName">The application identity used for compilation and persistent stores.</param>
/// <param name="ProjectName">The application project and solution file name.</param>
/// <param name="RootNamespace">The application C# root namespace.</param>
public sealed record CratisPlanOptions(string ApplicationName, string ProjectName, string RootNamespace)
{
    /// <summary>
    /// Gets the domain sub-path, such as Sales or Sales/Retail. Empty preserves existing placement.
    /// </summary>
    public string Domain { get; init; } = string.Empty;
}
