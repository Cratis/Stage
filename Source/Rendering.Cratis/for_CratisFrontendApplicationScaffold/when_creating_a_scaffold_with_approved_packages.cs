// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;
using Cratis.Scene.Engine.Packages;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisFrontendApplicationScaffold.given;
using Cratis.Stage.Rendering.Cratis.Scaffolding;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisFrontendApplicationScaffold;

public class when_creating_a_scaffold_with_approved_packages : a_current_frontend_scaffold
{
    System.Collections.Immutable.ImmutableArray<ArtifactRenderInput> _approved;

    void Because() => _approved = new CratisFrontendApplicationScaffold().Create(_request.WithApprovedPackages(an_approved_package_set.Set));

    string Approved(string path) => Encoding.UTF8.GetString(_approved.Single(input => PathOf(input) == path).Bytes.AsSpan());

    [Fact] void should_emit_the_approved_package_set_document() => ApprovedPackageSetJson.Write(an_approved_package_set.Set).ShouldEqual(Approved("approved-packages.json"));
    [Fact] void should_emit_the_loading_module() => _approved.Select(PathOf).ShouldContain(".frontend/approvedPackages.ts");
    [Fact] void should_install_the_approved_package_in_sorted_position() => JsonDocument.Parse(Approved("package.json")).RootElement.GetProperty("dependencies").EnumerateObject().Select(_ => _.Name).SequenceEqual(JsonDocument.Parse(Approved("package.json")).RootElement.GetProperty("dependencies").EnumerateObject().Select(_ => _.Name).Order(StringComparer.Ordinal)).ShouldBeTrue();
    [Fact] void should_install_the_approved_package_at_its_version() => JsonDocument.Parse(Approved("package.json")).RootElement.GetProperty("dependencies").GetProperty("@acme/scene.charts").GetString().ShouldEqual("1.2.0");
    [Fact] void should_keep_every_shipped_dependency() => DependencyNames(Approved("package.json")).Except(DependencyNames(Content("package.json"))).ShouldEqual(["@acme/scene.charts"]);
    [Fact] void should_import_the_approved_module_literally() => Approved(".frontend/approvedPackages.ts").ShouldContain("// @ts-ignore - an approved package may ship without type declarations\n    '@acme/scene.charts': () => import('@acme/scene.charts')");
    [Fact] void should_import_the_approved_style_sheets() => Approved(".frontend/approvedPackages.ts").ShouldContain("import '@acme/scene.charts/styles.css';");
    [Fact] void should_load_through_the_scene_loader() => Approved(".frontend/approvedPackages.ts").ShouldContain("loadApprovedPackages(approvedPackageSet, importers, { allowExecutableImports: true })");
    [Fact] void should_import_the_approved_component_loader_in_the_main_module() => Approved(".frontend/main.tsx").ShouldContain("import { loadApprovedComponents } from './approvedPackages';");
    [Fact] void should_load_the_approved_packages_before_rendering() => Approved(".frontend/main.tsx").IndexOf("await loadApprovedComponents()", StringComparison.Ordinal).ShouldBeLessThan(Approved(".frontend/main.tsx").IndexOf("ReactDOM.createRoot", StringComparison.Ordinal));
    [Fact] void should_render_with_the_approved_components() => Approved(".frontend/main.tsx").ShouldContain("<App components={{ ...approved.components, ...cratisComponentsRuntimePackage.components }} packageCatalog={approved.catalog} screenTemplates={approved.screenTemplates} dialogTemplates={approved.dialogTemplates} />");
    [Fact] void should_load_the_templates_of_the_approved_packages() => Approved(".frontend/approvedPackages.ts").ShouldContain("screenTemplates: loaded.bundles.flatMap(bundle => bundle.screenTemplates ?? [])");
    [Fact] void should_leave_the_scaffold_without_approved_packages_untouched() => _first.Select(PathOf).ShouldNotContain(".frontend/approvedPackages.ts");
    [Fact] void should_not_mention_approved_packages_without_any() => Content(".frontend/main.tsx").ShouldNotContain("approved");
    [Fact] void should_refuse_a_package_named_like_a_shipped_one() => Catch.Exception(() => _request.WithApprovedPackages(ApprovedPackageSetJson.Read(an_approved_package_set.Document.Replace("Acme.Charts", "core", StringComparison.Ordinal)).Set!)).ShouldBeOfExactType<InvalidCratisBackendApplicationScaffold>();
    [Fact] void should_refuse_a_package_imported_from_an_installed_npm_package() => Catch.Exception(() => _request.WithApprovedPackages(ApprovedPackageSetJson.Read(an_approved_package_set.Document.Replace("@acme/scene.charts", "react", StringComparison.Ordinal).Replace("react/styles.css", "react/x.css", StringComparison.Ordinal)).Set!)).ShouldBeOfExactType<InvalidCratisBackendApplicationScaffold>();

    static string[] DependencyNames(string packageJson) => [.. JsonDocument.Parse(packageJson).RootElement.GetProperty("dependencies").EnumerateObject().Select(_ => _.Name)];
}
