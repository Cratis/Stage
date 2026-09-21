// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisFrontendApplicationScaffold.given;
using Cratis.Stage.Rendering.Cratis.Scaffolding;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisFrontendApplicationScaffold;

public class when_creating_the_current_frontend_scaffold : a_current_frontend_scaffold
{
    static readonly string[] _expectedPaths =
    [
        ".frontend/index.css",
        ".frontend/index.html",
        ".frontend/main.tsx",
        ".frontend/tsconfig.json",
        ".frontend/tsconfig.node.json",
        ".frontend/vite.config.ts",
        ".gitignore",
        "package.json",
        "tsconfig.json"
    ];

    [Fact] void should_create_the_exact_frontend_roster_in_ordinal_order() => _first.Select(PathOf).SequenceEqual(_expectedPaths).ShouldBeTrue();
    [Fact] void should_version_every_input_with_the_scaffold_contract() => _first.All(input => input.Version == "1").ShouldBeTrue();
    [Fact] void should_repeat_the_same_input_names() => _second.Select(input => input.Name).SequenceEqual(_first.Select(input => input.Name)).ShouldBeTrue();
    [Fact] void should_repeat_the_same_input_hashes() => _second.Select(input => input.Sha256).SequenceEqual(_first.Select(input => input.Sha256)).ShouldBeTrue();
    [Fact] void should_repeat_the_same_input_bytes() => _second.Zip(_first).All(pair => pair.First.Bytes.SequenceEqual(pair.Second.Bytes)).ShouldBeTrue();
    [Fact] void should_encode_every_input_as_strict_utf8_without_a_byte_order_mark() => _first.All(IsUtf8WithoutByteOrderMark).ShouldBeTrue();
    [Fact] void should_normalize_every_input_to_line_feeds() => _first.All(input => !input.Bytes.Contains((byte)'\r')).ShouldBeTrue();
    [Fact] void should_end_every_input_with_exactly_one_line_feed_byte() => _first.All(EndsWithExactlyOneLineFeed).ShouldBeTrue();
    [Fact] void should_reject_a_missing_request_with_the_project_exception() => Catch.Exception(() => new CratisFrontendApplicationScaffold().Create(null!)).ShouldBeOfExactType<InvalidCratisBackendApplicationScaffold>();
    [Fact] void should_pin_every_dependency_exactly() => DependencyVersions().ShouldEqual(ExpectedDependencyVersions());
    /// <summary>
    /// The shell declares what a composed screen imports.
    /// </summary>
    /// <remarks>
    /// A planned scene payload and its binding module import the component library and the Scene packages, so a
    /// shell that omitted them would emit an application whose own generated code cannot resolve its imports.
    /// </remarks>
    [Fact] void should_reference_the_component_stack() => _first.Any(input => Text(input).Contains("@cratis/components", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_reference_the_scene_packages() => _first.Any(input => Text(input).Contains("@cratis/scene.components", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_mount_only_the_arc_provider() => Content(".frontend/main.tsx").ShouldContain("<Arc>");
    [Fact] void should_not_mount_the_components_provider() => Content(".frontend/main.tsx").ShouldNotContain("CratisComponentsProvider");
    [Fact] void should_load_the_metadata_reflection_polyfill_first() => Content(".frontend/main.tsx").Split('\n')[0].ShouldEqual("import 'reflect-metadata';");
    [Fact] void should_build_into_the_hosted_web_root() => Content(".frontend/vite.config.ts").ShouldContain("outDir: '../wwwroot'");
    [Fact] void should_emit_arc_metadata_for_the_frontend() => Content(".frontend/vite.config.ts").ShouldContain("EmitMetadataPlugin");
    [Fact] void should_proxy_every_backend_surface_while_developing() => ProxiedPaths().ShouldEqual("/.cratis|/api|/swagger");
    [Fact] void should_name_the_package_after_the_application() => PackageProperty("name").ShouldEqual("myapp");
    [Fact] void should_keep_the_package_private() => PackageJson().RootElement.GetProperty("private").GetBoolean().ShouldBeTrue();
    [Fact] void should_run_vite_through_the_frontend_configuration() => PackageScript("dev").ShouldEqual("vite --config .frontend/vite.config.ts");
    [Fact] void should_type_check_before_bundling() => PackageScript("build").ShouldEqual("tsc -b .frontend/tsconfig.json && vite build --config .frontend/vite.config.ts");
    [Fact] void should_extend_the_frontend_type_configuration_from_the_root() => Content("tsconfig.json").ShouldContain("\"extends\": \"./.frontend/tsconfig.json\"");
    [Fact] void should_type_check_the_bundler_configuration_through_the_referenced_project() => Content(".frontend/tsconfig.json").ShouldContain("\"path\": \"./tsconfig.node.json\"");
    [Fact] void should_type_the_react_shell_it_compiles() => DependencyVersions().ShouldContain("@types/react=");
    [Fact] void should_ignore_the_bundled_web_root_and_node_modules() => Content(".gitignore").ShouldContain("wwwroot/");
    [Fact] void should_not_emit_wildcard_range_or_latest_versions() => HasForbiddenValues().ShouldBeFalse();

    JsonDocument PackageJson() => JsonDocument.Parse(Content("package.json"));

    string PackageProperty(string name) => PackageJson().RootElement.GetProperty(name).GetString()!;

    string PackageScript(string name) => PackageJson().RootElement.GetProperty("scripts").GetProperty(name).GetString()!;

    string DependencyVersions()
    {
        using var document = PackageJson();
        var dependencies = document.RootElement.GetProperty("dependencies").EnumerateObject()
            .Concat(document.RootElement.GetProperty("devDependencies").EnumerateObject())
            .Select(property => $"{property.Name}={property.Value.GetString()}")
            .Order(StringComparer.Ordinal);
        return string.Join('|', dependencies);
    }

    static string ExpectedDependencyVersions() => string.Join(
        '|',
        new[]
        {
            "@cratis/arc=22.16.1",
            "@cratis/arc.react=22.16.1",
            "@cratis/arc.vite=22.16.1",
            "@cratis/components=4.9.0",
            "@cratis/fundamentals=7.19.3",
            "@cratis/scene.components=3.5.0",
            "@cratis/scene.engine=3.5.0",
            "@cratis/scene.model=3.5.0",
            "@cratis/scene.react=3.5.0",
            "@primereact/core=11.1.0",
            "@primereact/headless=11.1.0",
            "@primereact/hooks=11.1.0",
            "@primereact/styles=11.1.0",
            "@primereact/types=11.1.0",
            "@primeuix/themes=3.0.1",
            "@types/react=19.3.0",
            "@types/react-dom=19.3.0",
            "@vitejs/plugin-react=6.1.1",
            "primeicons=8.0.1",
            "primereact=11.1.0",
            "react=19.3.0",
            "react-dom=19.3.0",
            "react-router-dom=7.18.4",
            "reflect-metadata=0.2.2",
            "rxjs=7.8.2",
            "tsyringe=4.10.0",
            "typescript=7.0.2",
            "vite=8.3.0"
        }.Order(StringComparer.Ordinal));

    string ProxiedPaths() => string.Join(
        '|',
        Content(".frontend/vite.config.ts").Split('\n')
            .Select(line => line.Trim())
            .Where(line => line.StartsWith("'/", StringComparison.Ordinal))
            .Select(line => line[1..line.IndexOf('\'', 1)]));

    bool HasForbiddenValues()
    {
        using var document = PackageJson();
        var versions = document.RootElement.GetProperty("dependencies").EnumerateObject()
            .Concat(document.RootElement.GetProperty("devDependencies").EnumerateObject())
            .Select(property => property.Value.GetString()!);
        return versions.Any(version =>
            version.Contains('*', StringComparison.Ordinal) ||
            version.Contains('^', StringComparison.Ordinal) ||
            version.Contains('~', StringComparison.Ordinal) ||
            version.Contains('-', StringComparison.Ordinal) ||
            version.Contains("latest", StringComparison.OrdinalIgnoreCase));
    }

    static bool IsUtf8WithoutByteOrderMark(ArtifactRenderInput input)
    {
        _ = Text(input);
        return input.Bytes.Length < 3 || input.Bytes[0] != 0xef || input.Bytes[1] != 0xbb || input.Bytes[2] != 0xbf;
    }

    static bool EndsWithExactlyOneLineFeed(ArtifactRenderInput input) =>
        input.Bytes.Length > 1 && input.Bytes[^1] == (byte)'\n' && input.Bytes[^2] != (byte)'\n';
}
