// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Text.Json;
using System.Xml.Linq;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisBackendApplicationScaffold.given;
using Cratis.Stage.Rendering.Cratis.Scaffolding;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisBackendApplicationScaffold;

public class when_creating_the_current_scaffold : a_current_scaffold
{
    static readonly string[] _expectedPaths =
    [
        ".editorconfig",
        "Directory.Build.props",
        "Directory.Build.targets",
        "Directory.Packages.props",
        "GeneratedPolicyRegistration.cs",
        "MyApp.csproj",
        "MyApp.slnx",
        "Program.cs",
        "README.md",
        "appsettings.json",
        "docker-compose.yml"
    ];

    [Fact] void should_create_the_exact_backend_roster_in_ordinal_order() => _first.Select(PathOf).SequenceEqual(_expectedPaths).ShouldBeTrue();
    [Fact] void should_not_emit_repository_files() => _first.Select(PathOf).Any(path => path.StartsWith(".github/", StringComparison.Ordinal)).ShouldBeFalse();
    [Fact] void should_version_every_input_with_the_scaffold_contract() => _first.All(input => input.Version == "2").ShouldBeTrue();
    [Fact] void should_repeat_the_same_input_names() => _second.Select(input => input.Name).SequenceEqual(_first.Select(input => input.Name)).ShouldBeTrue();
    [Fact] void should_repeat_the_same_input_hashes() => _second.Select(input => input.Sha256).SequenceEqual(_first.Select(input => input.Sha256)).ShouldBeTrue();
    [Fact] void should_repeat_the_same_input_bytes() => _second.Zip(_first).All(pair => pair.First.Bytes.SequenceEqual(pair.Second.Bytes)).ShouldBeTrue();
    [Fact] void should_encode_every_input_as_strict_utf8_without_a_byte_order_mark() => _first.All(IsUtf8WithoutByteOrderMark).ShouldBeTrue();
    [Fact] void should_normalize_every_input_to_line_feeds() => _first.All(input => !input.Bytes.Contains((byte)'\r')).ShouldBeTrue();
    [Fact] void should_end_every_input_with_exactly_one_line_feed_byte() => _first.All(EndsWithExactlyOneLineFeed).ShouldBeTrue();
    [Fact] void should_stop_inheriting_parent_build_properties() => Content("Directory.Build.props").ShouldNotContain("<Import");
    [Fact] void should_configure_the_shared_build_policy() => BuildProperties().ShouldEqual("net10.0|latest|enable|enable|true|true");
    [Fact] void should_keep_only_environment_dependent_restore_diagnostics_as_warnings() => XDocument.Parse(Content("Directory.Build.props")).Descendants("WarningsNotAsErrors").Single().Value.ShouldEqual("$(WarningsNotAsErrors);NU1507;NU1900;NU1901;NU1902;NU1903;NU1904");
    [Fact] void should_not_hide_restore_diagnostics() => XDocument.Parse(Content("Directory.Build.props")).Descendants("NoWarn").ShouldBeEmpty();
    [Fact] void should_stop_inheriting_parent_build_targets() => Content("Directory.Build.targets").ShouldEqual("<Project />\n");
    [Fact] void should_enable_isolated_central_package_management_and_transitive_pins() => CentralPackageManagement().ShouldBeTrue();
    [Fact] void should_leave_all_package_references_unversioned() => UnversionedReferences().ShouldBeTrue();
    [Fact] void should_pin_the_profile_contract_and_target_framework() => $"{_profile.Version}|{_profile.TargetFramework}".ShouldEqual("2|net10.0");
    [Fact] void should_expose_only_the_verified_current_profile_as_public_static_surface() => PublicStaticProfileMethods().ShouldContainOnly("get_Current");
    [Fact] void should_emit_the_solution_without_a_guid() => SolutionSemantics().ShouldEqual("MyApp.csproj|False");
    [Fact] void should_emit_only_the_exact_package_versions() => PackageVersions().ShouldEqual(ExpectedPackageVersions());
    [Fact] void should_reference_all_specification_packages_only_in_debug() => TestingPackagesAreDebugOnly().ShouldBeTrue();
    [Fact] void should_not_emit_a_separate_specifications_project() => _first.Select(PathOf).Count(path => path.EndsWith(".csproj", StringComparison.Ordinal)).ShouldEqual(1);
    [Fact] void should_keep_the_host_active_in_debug() => Content("Program.cs").ShouldNotContain("#if");
    [Fact] void should_disable_reverse_extraction_of_the_authoritative_source_model_in_every_project() => XDocument.Parse(Content("Directory.Build.props")).Root!.Element("PropertyGroup")!.Element("CratisEmbeddedScreenplayEnabled")!.Value.ShouldEqual("false");
    [Fact] void should_not_duplicate_the_shared_extraction_setting_in_the_host() => Content("MyApp.csproj").ShouldNotContain("CratisEmbeddedScreenplayEnabled");
    [Fact] void should_not_suppress_the_test_entry_point_warning() => Content("MyApp.csproj").ShouldNotContain("NoWarn");
    [Fact] void should_mark_the_application_as_a_test_project_only_in_debug() => DebugProperty("IsTestProject").ShouldEqual("true");
    [Fact] void should_keep_the_application_entry_point_in_debug() => DebugProperty("GenerateProgramFile").ShouldEqual("false");
    [Fact] void should_remove_specifications_from_release_builds() => ReleaseCompileRemove().ShouldEqual("**/when_*.cs;**/when_*/**;**/for_*/**;**/given/**");
    [Fact] void should_place_using_directives_above_the_namespace() => Content(".editorconfig").ShouldContain("csharp_using_directive_placement = outside_namespace");
    [Fact] void should_isolate_editor_configuration_from_parent_rules() => Content(".editorconfig").ShouldContain("root = true");
    [Fact] void should_disable_ancestor_editor_configuration_discovery() => XDocument.Parse(Content("Directory.Build.props")).Descendants("DiscoverEditorConfigFiles").Single().Value.ShouldEqual("false");
    [Fact] void should_disable_ancestor_global_analyzer_configuration_discovery() => XDocument.Parse(Content("Directory.Build.props")).Descendants("DiscoverGlobalAnalyzerConfigFiles").Single().Value.ShouldEqual("false");
    [Fact] void should_include_the_application_editor_configuration_explicitly() => XDocument.Parse(Content("Directory.Build.props")).Descendants("EditorConfigFiles").Single().Attribute("Include")!.Value.ShouldEqual("$(MSBuildThisFileDirectory).editorconfig");
    [Fact] void should_allow_bdd_specification_names() => Content(".editorconfig").ShouldContain("[{**/when_*.cs,**/when_*/**/*.cs,**/for_*/**/*.cs,**/given/**/*.cs}]\n# Cratis specifications use BDD names, including should_ facts.\ndotnet_diagnostic.IDE1006.severity = none\ndotnet_diagnostic.CA1707.severity = none");
    [Fact] void should_describe_running_the_specifications_from_the_application_project() => Content("README.md").ShouldContain("dotnet test MyApp.csproj\n");
    [Fact] void should_describe_specifications_compiling_only_in_debug() => Content("README.md").ShouldContain("A Release build\nremoves them and needs no test packages.");
    [Fact] void should_describe_the_actual_frontend_dev_script() => Content("README.md").ShouldContain("yarn dev");
    [Fact] void should_enable_corepack_before_installing_with_the_pinned_yarn() => Content("README.md").ShouldContain("corepack enable\nyarn install\n");
    [Fact] void should_document_scaffold_ownership() => Content("README.md").ShouldContain("Scaffold files belong to you after first run");
    [Fact] void should_configure_cratis_with_mongodb_and_camel_case_chronicle_naming() => Content("Program.cs").ShouldContain("configureArcBuilder: arc => arc.WithMongoDB(),\n    configureChronicleBuilder: chronicle => chronicle.WithCamelCaseNamingPolicy()");
    [Fact] void should_activate_cratis_and_the_health_endpoint_before_running() => ProgramSemantics().ShouldEqual("True|True|True");
    [Fact] void should_emit_the_exact_arc_chronicle_and_mongodb_settings() => AppSettingsSemantics().ShouldEqual("api|False|1|MyApp|chronicle://chronicle-dev-client:chronicle-dev-secret@localhost:35000|mongodb://localhost:27017|MyApp");
    [Fact] void should_keep_event_store_data_in_named_volumes() => Content("docker-compose.yml").ShouldContain("- chronicle-data:/data/db");
    [Fact] void should_keep_event_store_configuration_in_named_volumes() => Content("docker-compose.yml").ShouldContain("- chronicle-config:/data/configdb");
    [Fact] void should_pin_the_compatible_development_chronicle_image() => Content("docker-compose.yml").ShouldContain($"image: cratis/chronicle:{_profile.ChronicleImageVersion}-development");
    [Fact] void should_expose_only_the_required_chronicle_and_mongodb_ports() => ComposePorts().ShouldEqual("27017:27017|35000:35000");
    [Fact] void should_not_emit_wildcard_range_latest_or_random_guid_values() => HasForbiddenValues().ShouldBeFalse();

    static IEnumerable<string> PublicStaticProfileMethods() =>
        typeof(CratisBackendApplicationScaffoldProfile)
            .GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Select(method => method.Name);

    string BuildProperties()
    {
        var props = XDocument.Parse(Content("Directory.Build.props")).Root!.Element("PropertyGroup")!;
        return string.Join('|', new[] { "TargetFramework", "LangVersion", "Nullable", "ImplicitUsings", "TreatWarningsAsErrors", "EnforceCodeStyleInBuild" }.Select(name => props.Element(name)!.Value));
    }

    bool CentralPackageManagement()
    {
        var props = XDocument.Parse(Content("Directory.Packages.props"));
        return props.Descendants("ManagePackageVersionsCentrally").Single().Value == "true" &&
               props.Descendants("CentralPackageTransitivePinningEnabled").Single().Value == "true" &&
               !props.Descendants("Import").Any();
    }

    bool UnversionedReferences() => _first.Where(input => PathOf(input).EndsWith(".csproj", StringComparison.Ordinal))
        .SelectMany(input => XDocument.Parse(Text(input)).Descendants("PackageReference"))
        .All(reference => reference.Attribute("Version") is null && reference.Element("Version") is null);

    string SolutionSemantics()
    {
        var solution = XDocument.Parse(Content("MyApp.slnx"));
        var paths = string.Join('|', solution.Root!.Elements("Project").Select(project => project.Attribute("Path")!.Value));
        return $"{paths}|{solution.ToString().Contains("Guid", StringComparison.OrdinalIgnoreCase)}";
    }

    string PackageVersions()
    {
        var project = XDocument.Parse(Content("Directory.Packages.props"));
        return string.Join(
            '|',
            project.Descendants("PackageVersion")
                .Select(reference => $"{reference.Attribute("Include")!.Value}={reference.Attribute("Version")!.Value}")
                .Order(StringComparer.Ordinal));
    }

    string ExpectedPackageVersions() => string.Join(
        '|',
        new[]
        {
            $"Cratis={_profile.CratisPackageVersion}",
            $"Cratis.Arc.Chronicle.Testing={_profile.CratisArcChronicleTestingPackageVersion}",
            $"Cratis.Arc.MongoDB={_profile.CratisArcMongoDBPackageVersion}",
            $"Cratis.Chronicle={_profile.CratisChroniclePackageVersion}",
            $"Cratis.Chronicle.AspNetCore={_profile.CratisChroniclePackageVersion}",
            $"Cratis.Chronicle.Testing={_profile.CratisChroniclePackageVersion}",
            $"Cratis.Screenplay.Matching={Scaffolding.ScaffoldPackageVersions.ScreenplayMatching}",
            $"Cratis.Specifications={_profile.CratisSpecificationsPackageVersion}",
            $"Cratis.Specifications.XUnit={_profile.CratisSpecificationsXUnitPackageVersion}",
            $"Microsoft.NET.Test.Sdk={_profile.MicrosoftNetTestSdkPackageVersion}",
            $"NSubstitute={_profile.NSubstitutePackageVersion}",
            $"xunit={_profile.XunitPackageVersion}",
            $"xunit.runner.visualstudio={_profile.XunitRunnerVisualStudioPackageVersion}"
        }.Order(StringComparer.Ordinal));

    bool TestingPackagesAreDebugOnly()
    {
        var project = XDocument.Parse(Content("MyApp.csproj"));
        var debug = project.Descendants("ItemGroup").Where(group => group.Attribute("Condition")?.Value == "'$(Configuration)' == 'Debug'").ToArray();
        return debug.Length == 1 &&
               debug[0].Elements("PackageReference").Select(reference => reference.Attribute("Include")!.Value).Order(StringComparer.Ordinal).SequenceEqual(
                   new[]
                   {
                       "Cratis.Arc.Chronicle.Testing",
                       "Cratis.Chronicle.Testing",
                       "Cratis.Specifications",
                       "Cratis.Specifications.XUnit",
                       "Microsoft.NET.Test.Sdk",
                       "NSubstitute",
                       "xunit",
                       "xunit.runner.visualstudio"
                   }.Order(StringComparer.Ordinal)) &&
               !project.Descendants("ItemGroup").Where(group => group.Attribute("Condition") is null)
                   .SelectMany(group => group.Elements("PackageReference"))
                   .Select(reference => reference.Attribute("Include")!.Value).Intersect(["xunit", "Microsoft.NET.Test.Sdk", "Cratis.Specifications"], StringComparer.Ordinal).Any();
    }

    string DebugProperty(string name) => XDocument.Parse(Content("MyApp.csproj")).Descendants("PropertyGroup")
        .Single(group => group.Attribute("Condition")?.Value == "'$(Configuration)' == 'Debug'").Element(name)!.Value;

    string ReleaseCompileRemove() => XDocument.Parse(Content("MyApp.csproj")).Descendants("ItemGroup")
        .Single(group => group.Attribute("Condition")?.Value == "'$(Configuration)' != 'Debug'").Element("Compile")!.Attribute("Remove")!.Value;

    string ProgramSemantics()
    {
        var program = Content("Program.cs");
        return string.Join(
            '|',
            program.Contains("app.UseCratis();", StringComparison.Ordinal),
            program.Contains("app.MapHealthChecks(\"/healthz\");", StringComparison.Ordinal),
            program.Contains("await app.RunAsync();", StringComparison.Ordinal));
    }

    string AppSettingsSemantics()
    {
        using var document = JsonDocument.Parse(Content("appsettings.json"));
        var cratis = document.RootElement.GetProperty("Cratis");
        var api = cratis.GetProperty("Arc").GetProperty("GeneratedApis");
        var chronicle = cratis.GetProperty("Chronicle");
        var mongoDB = cratis.GetProperty("MongoDB");
        return string.Join(
            '|',
            api.GetProperty("RoutePrefix").GetString(),
            api.GetProperty("IncludeCommandNameInRoute").GetBoolean(),
            api.GetProperty("SegmentsToSkipForRoute").GetInt32(),
            chronicle.GetProperty("EventStore").GetString(),
            chronicle.GetProperty("ConnectionString").GetString(),
            mongoDB.GetProperty("Server").GetString(),
            mongoDB.GetProperty("Database").GetString());
    }

    string ComposePorts() => string.Join(
        '|',
        Content("docker-compose.yml").Split('\n')
            .Select(line => line.Trim().TrimStart('-').Trim().Trim('"'))
            .Where(line => string.Equals(line, "27017:27017", StringComparison.Ordinal) ||
                           string.Equals(line, "35000:35000", StringComparison.Ordinal)));

    bool HasForbiddenValues()
    {
        var content = string.Join('\n', _first.Select(Text));
        var tokens = content.Split([' ', '\t', '\r', '\n', '"', '\'', '<', '>', '=', '/', ':', ';', '(', ')'], StringSplitOptions.RemoveEmptyEntries);
        return content.Contains("Version=\"*", StringComparison.Ordinal) ||
               content.Contains("Version=\"latest", StringComparison.OrdinalIgnoreCase) ||
               content.Contains("Version=\"[", StringComparison.Ordinal) ||
               tokens.Any(token => Guid.TryParse(token, out _));
    }

    static bool IsUtf8WithoutByteOrderMark(ArtifactRenderInput input)
    {
        _ = Text(input);
        return input.Bytes.Length < 3 || input.Bytes[0] != 0xef || input.Bytes[1] != 0xbb || input.Bytes[2] != 0xbf;
    }

    static bool EndsWithExactlyOneLineFeed(ArtifactRenderInput input) =>
        input.Bytes.Length > 1 && input.Bytes[^1] == (byte)'\n' && input.Bytes[^2] != (byte)'\n';
}
