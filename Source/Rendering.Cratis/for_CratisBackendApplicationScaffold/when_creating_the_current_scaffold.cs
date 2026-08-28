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
        ".gitignore",
        "Directory.Build.props",
        "Directory.Packages.props",
        "MyApp.csproj",
        "MyApp.slnx",
        "Program.cs",
        "appsettings.json",
        "docker-compose.yml"
    ];

    [Fact] void should_create_the_exact_backend_roster_in_ordinal_order() => _first.Select(PathOf).SequenceEqual(_expectedPaths).ShouldBeTrue();
    [Fact] void should_version_every_input_with_the_scaffold_contract() => _first.All(input => input.Version == "1").ShouldBeTrue();
    [Fact] void should_repeat_the_same_input_names() => _second.Select(input => input.Name).SequenceEqual(_first.Select(input => input.Name)).ShouldBeTrue();
    [Fact] void should_repeat_the_same_input_hashes() => _second.Select(input => input.Sha256).SequenceEqual(_first.Select(input => input.Sha256)).ShouldBeTrue();
    [Fact] void should_repeat_the_same_input_bytes() => _second.Zip(_first).All(pair => pair.First.Bytes.SequenceEqual(pair.Second.Bytes)).ShouldBeTrue();
    [Fact] void should_encode_every_input_as_strict_utf8_without_a_byte_order_mark() => _first.All(IsUtf8WithoutByteOrderMark).ShouldBeTrue();
    [Fact] void should_normalize_every_input_to_line_feeds() => _first.All(input => !input.Bytes.Contains((byte)'\r')).ShouldBeTrue();
    [Fact] void should_end_every_input_with_exactly_one_line_feed_byte() => _first.All(EndsWithExactlyOneLineFeed).ShouldBeTrue();
    [Fact] void should_stop_inheriting_parent_build_properties() => Content("Directory.Build.props").ShouldEqual("<Project />\n");
    [Fact] void should_disable_inherited_central_package_management() => DirectoryPackagesPropsDisablesCentralPackageManagement().ShouldBeTrue();
    [Fact] void should_pin_the_current_profile() => ProfileValues().ShouldEqual("1|net10.0|22.3.0|22.3.0|22.3.0|4.0.0|4.0.0|18.9.0|6.2.0|2.9.3|4.0.0|16.35.3");
    [Fact] void should_expose_only_the_verified_current_profile_as_public_static_surface() => PublicStaticProfileMethods().ShouldContainOnly("get_Current");
    [Fact] void should_emit_the_solution_without_a_guid() => SolutionSemantics().ShouldEqual("MyApp.csproj|False");
    [Fact] void should_emit_only_the_exact_package_versions() => PackageVersions().ShouldEqual(ExpectedPackageVersions());
    [Fact] void should_keep_all_specification_packages_in_the_debug_item_group() => TestingPackagesAreDebugOnly().ShouldBeTrue();
    [Fact] void should_disable_the_host_in_debug() => Content("Program.cs").ShouldContain("#if !DEBUG");
    [Fact] void should_configure_cratis_with_mongodb_and_camel_case_chronicle_naming() => Content("Program.cs").ShouldContain("configureArcBuilder: arc => arc.WithMongoDB(),\n    configureChronicleBuilder: chronicle => chronicle.WithCamelCaseNamingPolicy()");
    [Fact] void should_activate_cratis_and_the_health_endpoint_before_running() => ProgramSemantics().ShouldEqual("True|True|True");
    [Fact] void should_emit_the_exact_arc_chronicle_and_mongodb_settings() => AppSettingsSemantics().ShouldEqual("api|False|1|MyApp|chronicle://chronicle-dev-client:chronicle-dev-secret@localhost:35000|mongodb://localhost:27017|MyApp");
    [Fact] void should_pin_the_compatible_development_chronicle_image() => Content("docker-compose.yml").ShouldContain("image: cratis/chronicle:16.35.3-development");
    [Fact] void should_expose_only_the_required_chronicle_and_mongodb_ports() => ComposePorts().ShouldEqual("27017:27017|35000:35000");
    [Fact] void should_not_emit_wildcard_range_latest_or_random_guid_values() => HasForbiddenValues().ShouldBeFalse();

    static IEnumerable<string> PublicStaticProfileMethods() =>
        typeof(CratisBackendApplicationScaffoldProfile)
            .GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Select(method => method.Name);

    string ProfileValues() => string.Join(
        '|',
        _profile.Version,
        _profile.TargetFramework,
        _profile.CratisPackageVersion,
        _profile.CratisArcMongoDBPackageVersion,
        _profile.CratisArcChronicleTestingPackageVersion,
        _profile.CratisSpecificationsPackageVersion,
        _profile.CratisSpecificationsXUnitPackageVersion,
        _profile.MicrosoftNetTestSdkPackageVersion,
        _profile.NSubstitutePackageVersion,
        _profile.XunitPackageVersion,
        _profile.XunitRunnerVisualStudioPackageVersion,
        _profile.ChronicleImageVersion);

    bool DirectoryPackagesPropsDisablesCentralPackageManagement()
    {
        var props = XDocument.Parse(Content("Directory.Packages.props"));
        return props.Root!.Element("PropertyGroup")!.Element("ManagePackageVersionsCentrally")!.Value == "false";
    }

    string SolutionSemantics()
    {
        var solution = XDocument.Parse(Content("MyApp.slnx"));
        var project = solution.Root!.Element("Project")!;
        return $"{project.Attribute("Path")!.Value}|{solution.ToString().Contains("Guid", StringComparison.OrdinalIgnoreCase)}";
    }

    string PackageVersions()
    {
        var project = XDocument.Parse(Content("MyApp.csproj"));
        return string.Join(
            '|',
            project.Descendants("PackageReference")
                .Select(reference => $"{reference.Attribute("Include")!.Value}={reference.Attribute("Version")!.Value}")
                .Order(StringComparer.Ordinal));
    }

    static string ExpectedPackageVersions() => string.Join(
        '|',
        new[]
        {
            "Cratis=22.3.0",
            "Cratis.Arc.Chronicle.Testing=22.3.0",
            "Cratis.Arc.MongoDB=22.3.0",
            "Cratis.Specifications=4.0.0",
            "Cratis.Specifications.XUnit=4.0.0",
            "Microsoft.NET.Test.Sdk=18.9.0",
            "NSubstitute=6.2.0",
            "xunit=2.9.3",
            "xunit.runner.visualstudio=4.0.0"
        }.Order(StringComparer.Ordinal));

    bool TestingPackagesAreDebugOnly()
    {
        var project = XDocument.Parse(Content("MyApp.csproj"));
        var testing = project.Descendants("ItemGroup").Single(group => group.Attribute("Condition") is not null);
        return testing.Attribute("Condition")!.Value == "'$(Configuration)' == 'Debug'" &&
               testing.Elements("PackageReference").Select(reference => reference.Attribute("Include")!.Value).Order(StringComparer.Ordinal).SequenceEqual(
                   new[]
                   {
                       "Cratis.Arc.Chronicle.Testing",
                       "Cratis.Specifications",
                       "Cratis.Specifications.XUnit",
                       "Microsoft.NET.Test.Sdk",
                       "NSubstitute",
                       "xunit",
                       "xunit.runner.visualstudio"
                   }.Order(StringComparer.Ordinal));
    }

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
        return content.Contains('*') ||
               content.Contains("latest", StringComparison.OrdinalIgnoreCase) ||
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
