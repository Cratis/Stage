// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;
using Cratis.Screenplay.Extensions;
using Cratis.Specifications;
using Cratis.Stage.Host.Extensions.Packages.given;
using Xunit;

namespace Cratis.Stage.Host.Extensions.Packages.for_NpmPackageChannel;

public class when_downloading_an_npm_package : Specification
{
    const string Name = "@acme/charts";
    const string Version = "1.0.0";

    FakeRegistry _registry = null!;
    FetchedPackage _fetched = null!;
    SignatureVerdict _verdict;
    SignatureVerdict _verdictForOtherBytes;
    PackageRefusedException _mismatch = null!;
    PackageContents _contents = null!;

    static byte[] Metadata(string tarballUrl, byte[] tarball, string? signedFor, Func<byte[], byte[]> sign) => Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new
    {
        dist = new { tarball = tarballUrl, signatures = new[] { new { keyid = "SHA256:x", sig = Convert.ToBase64String(sign(Encoding.UTF8.GetBytes(signedFor ?? $"{Name}@{Version}:{PackageArtifact.Sha512Integrity(tarball)}"))) } } }
    }));

    async Task Because()
    {
        using var key = PackageFixtures.NewKey();
        var files = PackageFixtures.Files();
        var tarball = PackageFixtures.Archive("package", files);
        var entry = PackageFixtures.Entry(tarball, files["contract.play"], resolved: $"npm:{Name}") with { Version = Version };
        byte[] Sign(byte[] message) => key.SignData(message, System.Security.Cryptography.HashAlgorithmName.SHA256, System.Security.Cryptography.DSASignatureFormat.Rfc3279DerSequence);

        _registry = new();
        _registry.Put("/@acme%2Fcharts/1.0.0", Metadata("https://registry.npmjs.org/@acme/charts/-/charts-1.0.0.tgz", tarball, null, Sign));
        _registry.Put("/@acme/charts/-/charts-1.0.0.tgz", tarball);
        using var http = new HttpClient(_registry);
        var channel = new NpmPackageChannel(http, new());
        _fetched = channel.Open(entry, await channel.Download(entry, CancellationToken.None));
        _verdict = PackageSignatureVerifier.Verify(_fetched.Signatures, [PackageFixtures.Pem(key)], _fetched.CoversPackage);
        _contents = new PackageArchiveReader(10_000_000, 1000).Read(_fetched.Archives.Select(bytes => (Stream)new MemoryStream(bytes)), _fetched.ArchiveRoot);

        _registry.Put("/@acme%2Fcharts/1.0.0", Metadata("https://registry.npmjs.org/@acme/charts/-/charts-1.0.0.tgz", tarball, $"{Name}@{Version}:{PackageArtifact.Sha512Integrity([1])}", Sign));
        _verdictForOtherBytes = PackageSignatureVerifier.Verify(channel.Open(entry, await channel.Download(entry, CancellationToken.None)).Signatures, [PackageFixtures.Pem(key)], _fetched.CoversPackage);

        _registry.Put("/@acme/charts/-/charts-1.0.0.tgz", [1, 2, 3]);
        _mismatch = await Assert.ThrowsAsync<PackageRefusedException>(() => channel.Download(entry, CancellationToken.None));
    }

    [Fact] void should_read_the_package_from_the_package_folder() => _contents.Contract.ShouldNotBeNull();
    [Fact] void should_verify_the_registry_signature_over_the_name_version_and_integrity_of_what_was_downloaded() => _verdict.ShouldEqual(SignatureVerdict.Valid);
    [Fact] void should_not_accept_a_signature_over_other_bytes() => _verdictForOtherBytes.ShouldEqual(SignatureVerdict.Invalid);
    [Fact] void should_refuse_a_tarball_that_does_not_have_the_locked_digest() => _mismatch.Reason.ShouldEqual(PackageRefusalReason.DigestMismatch);
}
