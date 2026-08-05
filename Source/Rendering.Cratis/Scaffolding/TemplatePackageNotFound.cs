// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Cratis.Scaffolding;

/// <summary>
/// The exception that is thrown when the local NuGet cache does not contain the requested template package.
/// </summary>
/// <param name="packageId">The identifier of the missing template package.</param>
public class TemplatePackageNotFound(string packageId)
    : Exception($"Could not locate a restored '{packageId}' package in the local NuGet cache — ensure it is a package reference of the running application.");
