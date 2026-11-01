// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Contracts.Semantics;
using Cratis.Stage.Rendering.Cratis.Scaffolding;
using Cratis.Stage.Rendering.Cratis.Semantics;

#pragma warning disable MA0110 // The current SDK does not generate implementations for regex members in this project.
namespace Cratis.Stage.Rendering.Cratis.Realization;

/// <summary>
/// Adjusts the scaffold of an application whose model declares authentication or onboarding: it trusts the identity AuthProxy
/// forwards, honors the conventions of a Cratis runtime and, for onboarding, references Herald and Ante's contracts.
/// </summary>
internal static class RealizationScaffold
{
    static readonly Regex _chroniclePackage = new("(?<head>Include=\"Cratis\\.Chronicle(?:\\.AspNetCore|\\.Testing)?\" Version=\")(?<version>[^\"]+)\"", RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture, TimeSpan.FromMilliseconds(1000));
    static readonly Regex _chronicleImage = new("(?<head>cratis/chronicle:)(?<version>[0-9.]+)-development", RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture, TimeSpan.FromMilliseconds(1000));
    static readonly JsonSerializerOptions _indented = new() { WriteIndented = true, NewLine = "\n" };

    internal static PlannedArtifact Patch(PlannedArtifact artifact, ApplicationRealization realization, string rootNamespace)
    {
        var path = artifact.RelativePath;
        if (path.Contains('/', StringComparison.Ordinal))
        {
            return artifact;
        }

        Func<string, string>? patch = path switch
        {
            "Program.cs" => text => Program(text, realization, rootNamespace),
            "appsettings.json" => text => AppSettings(text, realization),
            "Directory.Packages.props" when realization.Onboarding is not null => PackageVersions,
            "docker-compose.yml" when realization.Onboarding is not null => ChronicleImage,
            _ when path.EndsWith(".csproj", StringComparison.Ordinal) && realization.Onboarding is not null => Project,
            _ => null
        };

        return patch is null ? artifact : PlannedArtifact.CreateText(path, patch(Encoding.UTF8.GetString(artifact.Bytes.AsSpan())), artifact.Sources);
    }

    static string Program(string text, ApplicationRealization realization, string rootNamespace)
    {
        text = InsertBefore(text, ProgramCratis(), $"{rootNamespace}.{RealizationArtifacts.AuthenticationFolder}.RuntimeConventions.Apply(builder);\n");
        return realization.Onboarding is null
            ? text
            : InsertBefore(text, ProgramServices(), $"{rootNamespace}.{RealizationArtifacts.OnboardingFolder}.Registration.Register(builder);\n");
    }

    // The application runs behind AuthProxy, which strips every identity header a client sends and writes its own, so the headers
    // can be trusted. They are not signed: the application must never be reachable except through AuthProxy.
    static string AppSettings(string text, ApplicationRealization realization)
    {
        if (RuntimeConfigurationFiles.SupportedProviders(realization).IsEmpty)
        {
            return text;
        }

        var root = JsonNode.Parse(text)!.AsObject();
        var arc = root["Cratis"]!.AsObject()["Arc"]!.AsObject();
        arc["TrustForwardedIdentityHeaders"] = true;

        return root.ToJsonString(_indented);
    }

    static string PackageVersions(string text)
    {
        const string required = ScaffoldPackageVersions.AnteChronicle;
        text = _chroniclePackage.Replace(text, match => AtLeast(match.Groups["version"].Value, required) ? match.Value : $"{match.Groups["head"].Value}{required}\"");

        return InsertBefore(
            text,
            "  </ItemGroup>",
            $"    <PackageVersion Include=\"Cratis.Ante.Contracts\" Version=\"{ScaffoldPackageVersions.AnteContracts}\" />\n" +
            $"    <PackageVersion Include=\"Cratis.Showrunner.Herald\" Version=\"{ScaffoldPackageVersions.Herald}\" />\n");
    }

    static string ChronicleImage(string text) =>
        _chronicleImage.Replace(text, match => AtLeast(match.Groups["version"].Value, ScaffoldPackageVersions.AnteChronicle) ? match.Value : $"{match.Groups["head"].Value}{ScaffoldPackageVersions.AnteChronicle}-development");

    // Herald reads the templates and images from the Emails folder next to the application, so they travel with it.
    static string Project(string text) => InsertBefore(
        text,
        "  <Import Project=\"Customizations/Dependencies.props\"",
        "  <ItemGroup>\n    <PackageReference Include=\"Cratis.Ante.Contracts\" />\n    <PackageReference Include=\"Cratis.Showrunner.Herald\" />\n  </ItemGroup>\n" +
        "  <ItemGroup>\n    <None Include=\"Emails/**\" CopyToOutputDirectory=\"PreserveNewest\" CopyToPublishDirectory=\"PreserveNewest\" />\n  </ItemGroup>\n");

    static bool AtLeast(string current, string required) =>
        Version.TryParse(current, out var have) && Version.TryParse(required, out var need) && have >= need;

    static string InsertBefore(string text, string marker, string insertion)
    {
        var index = text.IndexOf(marker, StringComparison.Ordinal);
        if (index < 0 || text.IndexOf(marker, index + marker.Length, StringComparison.Ordinal) >= 0)
        {
            throw UnsupportedSemanticRendering.For(nameof(RealizationScaffold), $"scaffold marker '{marker.Trim()}'");
        }

        return text.Insert(index, insertion);
    }

    static string ProgramCratis() => "builder.AddCratis(";

    static string ProgramServices() => "ConfigureServices(builder);\n";
}
