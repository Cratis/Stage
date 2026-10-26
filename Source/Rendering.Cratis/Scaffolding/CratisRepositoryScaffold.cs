// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.RegularExpressions;
using Cratis.Stage.Contracts.Rendering;

namespace Cratis.Stage.Rendering.Cratis.Scaffolding;

/// <summary>
/// Renders the repository root of a multi-service repository.
/// </summary>
internal static partial class CratisRepositoryScaffold
{
    const string Composition = "Composition";
    const string ChronicleResource = "chronicle";

    internal static string? Validate(CratisRepositoryPlanOptions options)
    {
        if (options is null) return "Repository planning requires options.";
        if (options.Services.IsDefaultOrEmpty) return "A repository requires at least one service.";
        if (!IsName(options.RepositoryName)) return $"The repository name '{options.RepositoryName}' must be a dot-separated C# identifier without paths or reserved keywords.";

        return options.Services.Select(ServiceError).FirstOrDefault(error => error is not null) ?? DuplicateError(options.Services);
    }

    static string? ServiceError(string service)
    {
        if (!IsName(service)) return $"The service name '{service}' must be a dot-separated C# identifier without paths or reserved keywords.";
        if (string.Equals(service, Composition, StringComparison.OrdinalIgnoreCase)) return $"The service name '{service}' is reserved for the Aspire application host.";

        // The application host adds Chronicle itself, and Aspire resource names are unique.
        return ResourceName(service) == ChronicleResource ? $"The service name '{service}' maps to the Aspire resource '{ChronicleResource}', which the application host reserves for Chronicle." : null;
    }

    static string? DuplicateError(IEnumerable<string> services)
    {
        if (services.GroupBy(service => service, StringComparer.OrdinalIgnoreCase).Any(group => group.Count() > 1)) return "Service names must be unique, ignoring case.";
        return services.GroupBy(ResourceName, StringComparer.Ordinal).Any(group => group.Count() > 1) ? "Two services map to the same Aspire resource name." : null;
    }

    internal static IEnumerable<PlannedArtifact> Render(CratisRepositoryPlanOptions options, CratisBackendApplicationScaffoldProfile profile)
    {
        var services = options.Services.Order(StringComparer.Ordinal).ToArray();
        var files = new (string Path, string Content)[]
        {
            (".editorconfig", CratisBackendApplicationScaffold.EditorConfig()),
            ("Directory.Build.props", CratisBackendApplicationScaffold.DirectoryBuildProps(profile)),
            ("Directory.Build.targets", CratisBackendApplicationScaffold.DirectoryBuildTargets()),
            ("Directory.Packages.props", DirectoryPackages(profile)),
            ($"{options.RepositoryName}.slnx", Solution(services)),
            ($"{ServiceLayout.Root}/{Composition}/{Composition}.csproj", CompositionProject(services)),
            ($"{ServiceLayout.Root}/{Composition}/Program.cs", CompositionProgram(services)),
            ("docker-compose.yml", CratisBackendApplicationScaffold.DockerCompose(profile))
        };

        return files.Select(file => PlannedArtifact.CreateText(file.Path, $"{file.Content.TrimEnd('\r', '\n')}\n"));
    }

    // The Aspire pins are the only difference from the single-service file.
    static string DirectoryPackages(CratisBackendApplicationScaffoldProfile profile)
    {
        var text = CratisBackendApplicationScaffold.DirectoryPackagesProps(profile);
        const string End = "  </ItemGroup>";
        var pins = $"    <PackageVersion Include=\"Aspire.Hosting.AppHost\" Version=\"{ScaffoldPackageVersions.AspireAppHost}\" />\n    <PackageVersion Include=\"Cratis.Chronicle.Aspire\" Version=\"{profile.CratisChroniclePackageVersion}\" />\n";
        var index = text.IndexOf(End, StringComparison.Ordinal);

        return string.Concat(text.AsSpan(0, index), pins, text.AsSpan(index));
    }

    static string Solution(string[] services)
    {
        var projects = services.Select(service => $"{ServiceLayout.Root}/{service}/{service}.csproj").Append($"{ServiceLayout.Root}/{Composition}/{Composition}.csproj").Order(StringComparer.Ordinal);
        return $"<Solution>\n{string.Concat(projects.Select(path => $"  <Project Path=\"{path}\" />\n"))}</Solution>";
    }

    static string CompositionProject(string[] services) =>
        $"""
        <Project Sdk="Microsoft.NET.Sdk">
          <Sdk Name="Aspire.AppHost.Sdk" Version="{ScaffoldPackageVersions.AspireAppHost}" />

          <PropertyGroup>
            <OutputType>Exe</OutputType>
            <IsAspireHost>true</IsAspireHost>
            <AspireUseCliBundle>true</AspireUseCliBundle>
          </PropertyGroup>

          <ItemGroup>
            <PackageReference Include="Aspire.Hosting.AppHost" />
            <PackageReference Include="Cratis.Chronicle.Aspire" />
          </ItemGroup>

          <ItemGroup>
        {string.Join("\n", services.Select(service => $"    <ProjectReference Include=\"../{service}/{service}.csproj\" />"))}
          </ItemGroup>
        </Project>
        """;

    // Follows the MongoDB branch of the CratisAspire template: the development image runs MongoDB inside the Chronicle container.
    static string CompositionProgram(string[] services)
    {
        var builder = new StringBuilder();
        builder.Append(
            """
            using Cratis.Chronicle.Aspire;

            var builder = DistributedApplication.CreateBuilder(args);

            // The development image runs MongoDB inside the Chronicle container. Exposing it lets every service
            // read the read models Chronicle projects there. Do not use the development image in production.
            var chronicle = builder
                .AddCratisChronicle()
                .WithEndpoint(targetPort: 27017, name: "mongodb");
            var mongoDB = chronicle.GetEndpoint("mongodb");

            // The Chronicle client reads Cratis:Chronicle:ConnectionString rather than Aspire's ConnectionStrings,
            // so hand it the address Aspire assigned, with the development client credentials.
            var chronicleGrpc = chronicle.GetEndpoint("grpc");
            """);

        foreach (var service in services)
        {
            builder.Append(
                $$"""


                builder.AddProject<Projects.{{service.Replace('.', '_')}}>("{{ResourceName(service)}}")
                    .WithReference(chronicle)
                    .WithEnvironment(
                        "Cratis__Chronicle__ConnectionString",
                        ReferenceExpression.Create($"chronicle://chronicle-dev-client:chronicle-dev-secret@{chronicleGrpc.Property(EndpointProperty.Host)}:{chronicleGrpc.Property(EndpointProperty.Port)}"))
                    .WithEnvironment(
                        "Cratis__MongoDB__Server",
                        ReferenceExpression.Create($"mongodb://{mongoDB.Property(EndpointProperty.Host)}:{mongoDB.Property(EndpointProperty.Port)}/?directConnection=true"))
                    .WaitFor(chronicle);
                """);
        }

        return builder.Append("\n\nbuilder.Build().Run();").ToString();
    }

    // Lower-cased, with every run of characters outside [a-z0-9] collapsed to one '-'; 'service-' keeps it from being empty or starting with a digit.
    static string ResourceName(string service)
    {
        var name = NonResourceCharacters().Replace(service.ToLowerInvariant(), "-").Trim('-');
        return name.Length == 0 || char.IsAsciiDigit(name[0]) ? $"service-{name}".TrimEnd('-') : name;
    }

    static bool IsName(string value)
    {
        try
        {
            CratisBackendApplicationScaffoldRequest.Create(value);
            return true;
        }
        catch (InvalidCratisBackendApplicationScaffold)
        {
            return false;
        }
    }

    [GeneratedRegex("[^a-z0-9]+")]
    private static partial Regex NonResourceCharacters();
}
