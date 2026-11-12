// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Host.Extensions.Packages;

/// <summary>
/// A file in a package that is not kept in memory.
/// </summary>
/// <param name="Path">The package-relative path.</param>
/// <param name="Length">The size in bytes.</param>
public sealed record PackageFileInfo(string Path, long Length);

/// <summary>
/// What a package holds. The backend is only listed, because it can be large and is unpacked to disk when something runs it; everything else is held.
/// </summary>
/// <param name="Files">The held files by package-relative path, such as <c lang="text">contract.play</c> and <c lang="text">frontend/index.js</c>.</param>
/// <param name="Backend">The files under <c lang="text">backend/</c>.</param>
public sealed record PackageContents(IReadOnlyDictionary<string, byte[]> Files, IReadOnlyList<PackageFileInfo> Backend)
{
    /// <summary>
    /// The folder of the backend in a package.
    /// </summary>
    public const string BackendFolder = "backend/";

    /// <summary>
    /// The path of the contract in a package.
    /// </summary>
    public const string ContractPath = "contract.play";

    /// <summary>
    /// Gets the contract, or null when the package has none.
    /// </summary>
    public byte[]? Contract => Files.GetValueOrDefault(ContractPath);

    /// <summary>
    /// Finds a held file.
    /// </summary>
    /// <param name="path">The package-relative path.</param>
    /// <returns>The bytes, or null.</returns>
    public byte[]? Find(string path) => Files.GetValueOrDefault(path);
}
