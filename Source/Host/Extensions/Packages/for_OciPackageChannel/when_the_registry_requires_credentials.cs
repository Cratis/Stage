// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Specifications;
using Cratis.Stage.Host.Extensions.Packages.given;
using Xunit;

namespace Cratis.Stage.Host.Extensions.Packages.for_OciPackageChannel;

public class when_the_registry_requires_credentials : Specification
{
    FakeRegistry _registry = null!;
    PackageRefusedException _withoutCredentials = null!;
    PackageArtifact _artifact = null!;

    async Task Because()
    {
        var files = PackageFixtures.Files();
        var archive = PackageFixtures.Archive("extension", files);
        _registry = new() { RequireToken = true };
        var manifest = _registry.Publish("acme/extension-mail", archive);
        var entry = PackageFixtures.Entry(archive, files["contract.play"], digest: FakeRegistry.Digest(manifest));

        using var http = new HttpClient(_registry);
        var anonymous = new ExtensionPackageOptions();
        _withoutCredentials = await Assert.ThrowsAsync<PackageRefusedException>(() => new OciPackageChannel(new(http, anonymous), anonymous).Download(entry, CancellationToken.None));

        _registry.Requests.Clear();
        _registry.Authorizations.Clear();
        var configured = new ExtensionPackageOptions();
        configured.Registries["ghcr.io"] = new() { Username = "reader", Password = "s3cret" };
        _artifact = await new OciPackageChannel(new(http, configured), configured).Download(entry, CancellationToken.None);
    }

    [Fact] void should_refuse_without_credentials_and_say_where_to_configure_them() => _withoutCredentials.Message.ShouldContain("Registries");
    [Fact] void should_get_a_token_with_the_configured_credentials() => _registry.Authorizations[_registry.Requests.IndexOf("/token")].ShouldEqual("Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes("reader:s3cret")));
    [Fact] void should_use_the_token_for_the_artifact() => _registry.Authorizations.ShouldContain("Bearer granted");
    [Fact] void should_download_the_artifact() => _artifact.Parts.Keys.ShouldContain("manifest.json");
}
