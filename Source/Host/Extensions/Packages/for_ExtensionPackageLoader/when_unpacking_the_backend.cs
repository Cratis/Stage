// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Extensions;
using Cratis.Specifications;
using Cratis.Stage.Host.Extensions.Packages.given;
using Xunit;

namespace Cratis.Stage.Host.Extensions.Packages.for_ExtensionPackageLoader;

public class when_unpacking_the_backend : a_package_loader
{
    string _target = null!;
    PackageRefusedException _tampered = null!;

    async Task Because()
    {
        _files = PackageFixtures.Files(PackageFixtures.Contract(extra: "  backend runtime dotnet 10 assembly \"server.dll\""));
        _files["backend/server.dll"] = [1, 2, 3];
        _files["backend/lib/native.so"] = [4, 5];
        Build();
        _result = await Load();
        _target = Path.Combine(_folder, "run");
        _result.Loaded[0].UnpackBackend(_target);

        File.WriteAllBytes(Path.Combine(_options.CacheDirectory, _entry.Digest.Replace(':', '-'), "archive"), [9]);
        _tampered = Assert.Throws<PackageRefusedException>(() => _result.Loaded[0].UnpackBackend(Path.Combine(_folder, "second")));
    }

    [Fact] void should_load_the_package() => _result.Loaded.Count.ShouldEqual(1);
    [Fact] void should_unpack_the_backend() => File.ReadAllBytes(Path.Combine(_target, "server.dll")).SequenceEqual((byte[])[1, 2, 3]).ShouldBeTrue();
    [Fact] void should_unpack_nested_files() => File.Exists(Path.Combine(_target, "lib", "native.so")).ShouldBeTrue();
    [Fact] void should_not_unpack_the_frontend() => File.Exists(Path.Combine(_target, "index.js")).ShouldBeFalse();
    [Fact] void should_verify_the_bytes_again_before_unpacking() => _tampered.Reason.ShouldEqual(PackageRefusalReason.DigestMismatch);
}
