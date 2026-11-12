// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Extensions;
using Cratis.Specifications;
using Cratis.Stage.Host.Extensions.Packages.given;
using Xunit;

namespace Cratis.Stage.Host.Extensions.Packages.for_OciPackageChannel;

public class when_the_registry_answers_with_another_manifest : Specification
{
    PackageRefusedException _swapped = null!;
    PackageRefusedException _layer = null!;
    PackageRefusedException _cache = null!;
    PackageRefusedException _missing = null!;

    async Task Because()
    {
        var files = PackageFixtures.Files();
        var archive = PackageFixtures.Archive("extension", files);
        var registry = new FakeRegistry();
        var manifest = registry.Publish("acme/extension-mail", archive);
        var entry = PackageFixtures.Entry(archive, files["contract.play"], digest: FakeRegistry.Digest(manifest));
        using var http = new HttpClient(registry);
        var channel = new OciPackageChannel(new(http, new()), new());

        var other = PackageFixtures.Archive("extension", PackageFixtures.Files(PackageFixtures.Contract(extra: "  # evil")));
        var otherManifest = new FakeRegistry();
        var evil = otherManifest.Publish("acme/extension-mail", other);
        registry.Put($"/v2/acme/extension-mail/manifests/{entry.Digest}", evil);
        _swapped = await Assert.ThrowsAsync<PackageRefusedException>(() => channel.Download(entry, CancellationToken.None));

        registry.Put($"/v2/acme/extension-mail/manifests/{entry.Digest}", manifest);
        registry.Put($"/v2/acme/extension-mail/blobs/{FakeRegistry.Digest(archive)}", other);
        _layer = await Assert.ThrowsAsync<PackageRefusedException>(() => channel.Download(entry, CancellationToken.None));

        registry.Put($"/v2/acme/extension-mail/blobs/{FakeRegistry.Digest(archive)}", archive);
        var artifact = await channel.Download(entry, CancellationToken.None);
        var tampered = new PackageArtifact(artifact.Parts.ToDictionary(part => part.Key, part => part.Key.StartsWith("blob-") ? other : part.Value));
        _cache = Assert.Throws<PackageRefusedException>(() => channel.Open(entry, tampered));

        var absent = entry with { Digest = "sha256:" + new string('e', 64) };
        _missing = await Assert.ThrowsAsync<PackageRefusedException>(() => channel.Download(absent, CancellationToken.None));
    }

    [Fact] void should_refuse_a_manifest_that_does_not_have_the_locked_digest() => _swapped.Reason.ShouldEqual(PackageRefusalReason.DigestMismatch);
    [Fact] void should_refuse_a_layer_that_does_not_have_the_digest_in_the_manifest() => _layer.Reason.ShouldEqual(PackageRefusalReason.DigestMismatch);
    [Fact] void should_refuse_cached_bytes_that_were_changed() => _cache.Reason.ShouldEqual(PackageRefusalReason.DigestMismatch);
    [Fact] void should_refuse_a_digest_the_registry_does_not_have() => _missing.Reason.ShouldEqual(PackageRefusalReason.NotFound);
}
