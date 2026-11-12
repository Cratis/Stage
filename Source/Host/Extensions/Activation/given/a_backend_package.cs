// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Host.Extensions.Packages;
using Cratis.Stage.Host.Extensions.Packages.given;

namespace Cratis.Stage.Host.Extensions.Activation.given;

/// <summary>
/// A specification that loads a package with a backend of a chosen runtime.
/// </summary>
public abstract class a_backend_package : a_package_loader
{
    /// <summary>
    /// Loads the package with a backend.
    /// </summary>
    /// <param name="runtime">The runtime of the backend.</param>
    /// <param name="activation">An <c lang="text">activation</c> line of the contract, or empty.</param>
    /// <returns>The loaded package.</returns>
    protected async Task<LoadedExtensionPackage> LoadBackend(string runtime = "dotnet", string activation = "")
    {
        var extra = $"  backend runtime {runtime} 10 assembly \"server.dll\"" + (activation.Length > 0 ? $"\n  {activation}" : string.Empty);
        _files = PackageFixtures.Files(PackageFixtures.Contract(extra: extra));
        _files["backend/server.dll"] = [1, 2, 3];
        Build();
        _result = await Load();
        return _result.Loaded.Single();
    }

    /// <summary>
    /// Loads the package with no backend, as a frontend-only extension.
    /// </summary>
    /// <returns>The loaded package.</returns>
    protected async Task<LoadedExtensionPackage> LoadFrontendOnly()
    {
        Build();
        _result = await Load();
        return _result.Loaded.Single();
    }
}
