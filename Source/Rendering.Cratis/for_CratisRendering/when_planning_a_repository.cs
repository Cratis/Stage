// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRendering;

public class when_planning_a_repository : Specification
{
    CratisPlanResult _result = null!;
    CratisPlanResult _scaffold = null!;
    CratisPlanResult _reordered = null!;

    void Because()
    {
        _result = CratisRendering.PlanRepository(new("Acme", ["Orders", "Billing"]));
        _reordered = CratisRendering.PlanRepository(new("Acme", ["Billing", "Orders"]));
        _scaffold = CratisRendering.PlanScaffold(new("Acme", "Acme", "Acme"));
    }

    [Fact] void should_succeed() => _result.Success.ShouldBeTrue();
    [Fact] void should_plan_the_repository_files_in_ordinal_order() => string.Join("\n", _result.Artifacts.Select(artifact => artifact.RelativePath)).ShouldEqual(
        ".editorconfig\nAcme.slnx\nDirectory.Build.props\nDirectory.Build.targets\nDirectory.Packages.props\nSource/Composition/Composition.csproj\nSource/Composition/Program.cs\ndocker-compose.yml");
    [Fact] void should_plan_the_same_files_whatever_the_service_order() => _reordered.Digest.ShouldEqual(_result.Digest);

    [Theory]
    [InlineData(".editorconfig")]
    [InlineData("Directory.Build.props")]
    [InlineData("Directory.Build.targets")]
    [InlineData("docker-compose.yml")]
    public void should_share_the_files_of_the_single_service_scaffold(string path) => Text(_result, path).ShouldEqual(Text(_scaffold, path));

    [Fact]
    void should_add_only_the_aspire_pins_to_the_central_packages()
    {
        var expected = Text(_scaffold, "Directory.Packages.props")
            .Replace("  </ItemGroup>", $"    <PackageVersion Include=\"Aspire.Hosting.AppHost\" Version=\"{Scaffolding.ScaffoldPackageVersions.AspireAppHost}\" />\n    <PackageVersion Include=\"Cratis.Chronicle.Aspire\" Version=\"{Scaffolding.CratisBackendApplicationScaffoldProfile.Current.CratisChroniclePackageVersion}\" />\n  </ItemGroup>", StringComparison.Ordinal);
        Text(_result, "Directory.Packages.props").ShouldEqual(expected);
    }

    [Fact]
    void should_list_every_service_and_the_composition_in_the_solution() => Text(_result, "Acme.slnx").ShouldEqual(
        """
        <Solution>
          <Project Path="Source/Billing/Billing.csproj" />
          <Project Path="Source/Composition/Composition.csproj" />
          <Project Path="Source/Orders/Orders.csproj" />
        </Solution>

        """);

    [Fact]
    void should_reference_every_service_from_the_composition_project() => Text(_result, "Source/Composition/Composition.csproj").ShouldEqual(
        $$"""
        <Project Sdk="Microsoft.NET.Sdk">
          <Sdk Name="Aspire.AppHost.Sdk" Version="{{Scaffolding.ScaffoldPackageVersions.AspireAppHost}}" />

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
            <ProjectReference Include="../Billing/Billing.csproj" />
            <ProjectReference Include="../Orders/Orders.csproj" />
          </ItemGroup>
        </Project>

        """);

    [Fact]
    void should_compose_every_service_with_chronicle() => Text(_result, "Source/Composition/Program.cs").ShouldEqual(
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

        builder.AddProject<Projects.Billing>("billing")
            .WithReference(chronicle)
            .WithEnvironment(
                "Cratis__Chronicle__ConnectionString",
                ReferenceExpression.Create($"chronicle://chronicle-dev-client:chronicle-dev-secret@{chronicleGrpc.Property(EndpointProperty.Host)}:{chronicleGrpc.Property(EndpointProperty.Port)}"))
            .WithEnvironment(
                "Cratis__MongoDB__Server",
                ReferenceExpression.Create($"mongodb://{mongoDB.Property(EndpointProperty.Host)}:{mongoDB.Property(EndpointProperty.Port)}/?directConnection=true"))
            .WaitFor(chronicle);

        builder.AddProject<Projects.Orders>("orders")
            .WithReference(chronicle)
            .WithEnvironment(
                "Cratis__Chronicle__ConnectionString",
                ReferenceExpression.Create($"chronicle://chronicle-dev-client:chronicle-dev-secret@{chronicleGrpc.Property(EndpointProperty.Host)}:{chronicleGrpc.Property(EndpointProperty.Port)}"))
            .WithEnvironment(
                "Cratis__MongoDB__Server",
                ReferenceExpression.Create($"mongodb://{mongoDB.Property(EndpointProperty.Host)}:{mongoDB.Property(EndpointProperty.Port)}/?directConnection=true"))
            .WaitFor(chronicle);

        builder.Build().Run();

        """);

    [Fact] void should_use_short_names() => _result.Artifacts.Any(artifact => Encoding.UTF8.GetString(artifact.Bytes.AsSpan()).Contains("global::", StringComparison.Ordinal)).ShouldBeFalse();

    [Theory]
    [InlineData("Acme", "")]
    [InlineData("", "Orders")]
    [InlineData("Acme", "Composition")]
    [InlineData("Acme", "not valid")]
    [InlineData("Acme", "class")]
    [InlineData("a/b", "Orders")]
    [InlineData("Acme", "Chronicle")]
    [InlineData("Acme", "chronicle")]
    [InlineData("Acme", "CHRONICLE")]
    public void should_refuse_invalid_names(string repository, string service) => ShouldRefuse(CratisRendering.PlanRepository(new(repository, [service])));

    [Fact] void should_refuse_no_services() => ShouldRefuse(CratisRendering.PlanRepository(new("Acme", [])));
    [Fact] void should_refuse_services_that_differ_only_in_case() => ShouldRefuse(CratisRendering.PlanRepository(new("Acme", ["Orders", "orders"])));
    [Fact] void should_refuse_services_with_the_same_resource_name() => ShouldRefuse(CratisRendering.PlanRepository(new("Acme", ["My.Service", "My_Service"])));

    static string Text(CratisPlanResult result, string path) => Encoding.UTF8.GetString(result.Artifacts.Single(artifact => artifact.RelativePath == path).Bytes.AsSpan());

    static void ShouldRefuse(CratisPlanResult result)
    {
        result.Success.ShouldBeFalse();
        result.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain("STAGE-PLAN-029");
        result.Artifacts.ShouldBeEmpty();
    }
}
