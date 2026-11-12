// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Extensions;
using Cratis.Specifications;
using Cratis.Stage.Host.Extensions.Packages.given;
using Xunit;

namespace Cratis.Stage.Host.Extensions.Packages.for_OciPackageChannel;

public class when_downloading_an_artifact : Specification
{
    FakeRegistry _registry = null!;
    ExtensionLockEntry _entry = null!;
    FetchedPackage _fetched = null!;
    PackageContents _contents = null!;
    SignatureVerdict _verdict;

    async Task Because()
    {
        using var key = PackageFixtures.NewKey();
        var files = PackageFixtures.Files();
        var archive = PackageFixtures.Archive("extension", files);
        _registry = new();
        _entry = PackageFixtures.Entry(archive, files["contract.play"], digest: "sha256:" + new string('0', 64));

        // Publish first to learn the manifest digest, then sign for it.
        var manifest = _registry.Publish("acme/extension-mail", archive);
        _entry = _entry with { Digest = FakeRegistry.Digest(manifest) };
        var payload = PackageFixtures.CosignPayload(_entry.Digest);
        _registry.Publish("acme/extension-mail", archive, payload, PackageFixtures.Sign(key, payload).Signature);

        using var http = new HttpClient(_registry);
        var channel = new OciPackageChannel(new(http, new()), new());
        var artifact = await channel.Download(_entry, CancellationToken.None);
        _fetched = channel.Open(_entry, artifact);
        _contents = new PackageArchiveReader(10_000_000, 1000).Read(_fetched.Archives.Select(bytes => (Stream)new MemoryStream(bytes)), _fetched.ArchiveRoot);
        _verdict = PackageSignatureVerifier.Verify(_fetched.Signatures, [PackageFixtures.Pem(key)], _fetched.CoversPackage);
    }

    [Fact] void should_ask_for_the_manifest_by_its_locked_digest() => _registry.Requests.ShouldContain($"/v2/acme/extension-mail/manifests/{_entry.Digest}");
    [Fact] void should_never_ask_for_a_tag_or_a_range() => _registry.Requests.Where(path => path.Contains("/manifests/")).All(path => path.Contains("sha256")).ShouldBeTrue();
    [Fact] void should_read_the_package_from_the_layer() => _contents.Contract.ShouldNotBeNull();
    [Fact] void should_find_the_signature_pushed_next_to_the_artifact() => _fetched.Signatures.Count.ShouldEqual(1);
    [Fact] void should_have_a_signature_that_verifies_for_exactly_this_artifact() => _verdict.ShouldEqual(SignatureVerdict.Valid);
}
