// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Scene.Engine.Packages;

namespace Cratis.Stage.Rendering.Cratis.for_CratisFrontendApplicationScaffold.given;

/// <summary>
/// The approved package set of the custom package fixture the CLI and Stage render against.
/// </summary>
public static class an_approved_package_set
{
    /// <summary>
    /// The document as an author writes it.
    /// </summary>
    public const string Document =
        "{ \"version\": 1, \"packages\": [" +
        "{ \"manifest\": { \"name\": \"Acme.Charts\", \"version\": \"1.2.0\", \"kind\": \"ComponentLibrary\", \"components\": [\"chart\"] }," +
        " \"module\": { \"npm\": \"@acme/scene.charts\", \"version\": \"1.2.0\", \"export\": \"acmeChartsRuntimePackage\", \"styles\": [\"@acme/scene.charts/styles.css\"] } }" +
        "] }";

    /// <summary>
    /// Gets the set read from <see cref="Document"/>.
    /// </summary>
    public static ApprovedPackageSet Set => ApprovedPackageSetJson.Read(Document, CratisRendering.ShippedPackageNames).Set!;
}
