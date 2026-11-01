// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Host.Published;
using Xunit;

namespace Cratis.Stage.Host.Export.for_StageExport;

public class when_exporting_a_model : given.an_exportable_model
{
    string _folder = null!;
    string _second = null!;
    PackageManifest _manifest = null!;
    string _dockerfile = null!;

    async Task Because()
    {
        _folder = await StageExport.Run(Request(Output("one")) with { ModelReferences = new Dictionary<string, string> { ["module:Shop"] = "studio-module-1", ["slice:Shop.Orders.PlaceOrder"] = "studio-slice-9" } });
        _second = await StageExport.Run(Request(Output("two")) with { ModelReferences = new Dictionary<string, string> { ["slice:Shop.Orders.PlaceOrder"] = "studio-slice-9", ["module:Shop"] = "studio-module-1" } });
        _manifest = PackageManifestJson.Read(await File.ReadAllTextAsync(Path.Combine(_folder, "manifest.json")));
        _dockerfile = await File.ReadAllTextAsync(Path.Combine(_folder, "Dockerfile"));
    }

    [Fact] void should_name_the_published_application() => _manifest.PublishedName.ShouldEqual("my-shop");
    [Fact] void should_carry_the_application_version() => _manifest.ApplicationVersion.ShouldEqual("1.2.0");
    [Fact] void should_require_this_runtime() => _manifest.StageRuntimeVersion.ShouldEqual(StageRuntimeVersion.Current);
    [Fact] void should_not_enable_notes_unless_asked() => _manifest.NotesEnabled.ShouldBeFalse();
    [Fact] void should_require_sign_in() => _manifest.Authentication.Required.ShouldBeTrue();
    [Fact] void should_list_the_policies() => _manifest.Authentication.Policies.ShouldContainOnly("Members");
    [Fact] void should_list_the_roles() => _manifest.Authentication.Roles.ShouldContainOnly("Member");
    [Fact] void should_map_the_references() => _manifest.ModelReferences.Select(reference => $"{reference.Kind}:{reference.Name}={reference.Id}").ShouldContainOnly("module:Shop=studio-module-1", "slice:Shop.Orders.PlaceOrder=studio-slice-9");
    [Fact] void should_copy_the_model() => File.Exists(Path.Combine(_folder, "model", "shop.play")).ShouldBeTrue();
    [Fact] void should_digest_the_model() => _manifest.ModelDigest.ShouldEqual(PackageDigest.Of(Path.Combine(_folder, "model")));
    [Fact] void should_build_the_image_from_the_pinned_runtime() => _dockerfile.ShouldContain($"FROM cratis/stage:{StageRuntimeVersion.Current}");
    [Fact] void should_bake_the_package_in_at_the_known_path() => _dockerfile.ShouldContain("COPY model /stage/package/model");
    [Fact] void should_write_identical_manifests_for_identical_input() => File.ReadAllText(Path.Combine(_second, "manifest.json")).ShouldEqual(File.ReadAllText(Path.Combine(_folder, "manifest.json")));
    [Fact] void should_write_identical_dockerfiles() => File.ReadAllText(Path.Combine(_second, "Dockerfile")).ShouldEqual(_dockerfile);
    [Fact] void should_read_back_as_a_package() => PackageManifestJson.Write(PublishedPackage.Read(_folder).Manifest).ShouldEqual(PackageManifestJson.Write(_manifest));
}
