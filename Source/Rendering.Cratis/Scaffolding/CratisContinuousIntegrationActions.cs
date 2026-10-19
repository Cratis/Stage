// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Cratis.Scaffolding;

/// <summary>
/// Holds the GitHub Actions pinned by the scaffolded workflow, each by full commit hash with its release tag.
/// </summary>
static class CratisContinuousIntegrationActions
{
    /// <summary>
    /// The checkout step reference.
    /// </summary>
    public const string Checkout = "actions/checkout@3d3c42e5aac5ba805825da76410c181273ba90b1 # v7.0.1";

    /// <summary>
    /// The .NET SDK setup step reference.
    /// </summary>
    public const string SetupDotnet = "actions/setup-dotnet@a98b56852c35b8e3190ac28c8c2271da59106c68 # v6.0.0";

    /// <summary>
    /// The Node.js setup step reference.
    /// </summary>
    public const string SetupNode = "actions/setup-node@820762786026740c76f36085b0efc47a31fe5020 # v7.0.0";

    /// <summary>
    /// The Node.js version the frontend job builds with.
    /// </summary>
    public const string NodeVersion = "24";
}
