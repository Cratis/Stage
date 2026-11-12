// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using Cratis.Screenplay.Extensions;
using Cratis.Specifications;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Stage.Host.Extensions.Packages.given;

/// <summary>
/// A package signed by its publisher, a deployment that allows the publisher, and a loader over a fake channel.
/// </summary>
public abstract class a_package_loader : Specification
{
    protected string _folder = null!;
    protected ECDsa _key = null!;
    protected Dictionary<string, byte[]> _files = null!;
    protected byte[] _archive = null!;
    protected ExtensionLockEntry _entry = null!;
    protected ExtensionPackageOptions _options = null!;
    protected ProjectExtensionPolicy _project = ProjectExtensionPolicy.None;
    protected FakeChannel _channel = null!;
    protected ExtensionPackageSet _result = null!;
    protected IReadOnlyList<PackageSignature> _signatures = [];

    protected void Establish()
    {
        _folder = Path.Combine(Path.GetTempPath(), "stage-packages-" + Guid.NewGuid().ToString("N"));
        _key = PackageFixtures.NewKey();
        _files = PackageFixtures.Files();
        _options = new() { CacheDirectory = Path.Combine(_folder, "cache") };
        _options.Allow.Publishers = [PackageFixtures.Publisher];
        _options.Publishers[PackageFixtures.Publisher] = new() { Keys = [PackageFixtures.Pem(_key)] };
    }

    protected void Destroy()
    {
        _key.Dispose();
        if (Directory.Exists(_folder)) Directory.Delete(_folder, recursive: true);
    }

    protected void Build(bool sign = true)
    {
        _archive = PackageFixtures.Archive("extension", _files);
        _entry = PackageFixtures.Entry(_archive, _files["contract.play"]);
        _signatures = sign ? [PackageFixtures.Sign(_key, PackageFixtures.CosignPayload(_entry.Digest))] : [];
        _channel = new(_archive, "extension", _signatures);
    }

    protected Task<ExtensionPackageSet> Load() => Loader().Load(ExtensionLock.Empty.With(_entry));

    protected ExtensionPackageLoader Loader() => new(
        [_channel],
        new(_options.CacheDirectory),
        new(_options, _project),
        new(PackageFixtures.StageVersion, PackageFixtures.ScreenplayVersion),
        _options,
        new(Path.Combine(_folder, "contracts")),
        NullLogger.Instance);

    protected PackageRefusal Refusal => _result.Refusals.Single();
}
