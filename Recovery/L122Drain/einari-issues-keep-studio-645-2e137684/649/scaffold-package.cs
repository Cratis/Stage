using System;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.InteropServices;
using Cratis.Stage.Contracts.Rendering;

namespace Cratis.Stage.Rendering.Cratis.Scaffolding;

/// <summary>
/// Creates the deterministic in-memory inputs for a first-run Cratis backend application.
/// </summary>
public sealed class CratisBackendApplicationScaffold
{
	/// <summary>
	/// Creates the complete backend application scaffold without writing to a file system.
	/// </summary>
	/// <param name="request">The validated scaffold request.</param>
	/// <returns>Nine normalized UTF-8 text inputs in ordinal relative-path order.</returns>
	/// <exception cref="T:Cratis.Stage.Rendering.Cratis.Scaffolding.InvalidCratisBackendApplicationScaffold">Thrown when the request is missing.</exception>
	public ImmutableArray<ArtifactRenderInput> Create(CratisBackendApplicationScaffoldRequest request)
	{
		if (request == null)
		{
			throw new InvalidCratisBackendApplicationScaffold("A backend application scaffold requires a request.");
		}
		CratisBackendApplicationScaffoldProfile profile = request.Profile;
		return ImmutableCollectionsMarshal.AsImmutableArray((from artifact in new(string, string)[9]
			{
				("Directory.Build.props", DirectoryBuildProps()),
				("Directory.Build.targets", DirectoryBuildTargets()),
				("Directory.Packages.props", DirectoryPackagesProps()),
				(request.ProjectName + ".csproj", Project(request)),
				(request.ProjectName + ".slnx", Solution(request)),
				("Program.cs", Program(request)),
				("GeneratedPolicyRegistration.cs", PolicyRegistration(request)),
				("appsettings.json", AppSettings(request)),
				("docker-compose.yml", DockerCompose(profile))
			}.OrderBy<(string, string), string>(((string RelativePath, string Content) artifact) => artifact.RelativePath, StringComparer.Ordinal)
			select Input(artifact.RelativePath, profile.Version, artifact.Content)).ToArray());
	}

	private static ArtifactRenderInput Input(string relativePath, string version, string content)
	{
		string content2 = content.TrimEnd('\r', '\n') + "\n";
		return CratisArtifactRenderInput.CreateText(relativePath, version, content2);
	}

	private static string DirectoryBuildProps()
	{
		return "<Project />";
	}

	private static string DirectoryBuildTargets()
	{
		return "<Project />";
	}

	private static string DirectoryPackagesProps()
	{
		return "<Project>\n  <PropertyGroup>\n    <ManagePackageVersionsCentrally>false</ManagePackageVersionsCentrally>\n  </PropertyGroup>\n</Project>";
	}

	private static string Solution(CratisBackendApplicationScaffoldRequest request)
	{
		return "<Solution>\n  <Project Path=\"" + request.ProjectName + ".csproj\" />\n</Solution>";
	}

	private static string Project(CratisBackendApplicationScaffoldRequest request)
	{
		CratisBackendApplicationScaffoldProfile profile = request.Profile;
		return $"<Project Sdk=\"Microsoft.NET.Sdk.Web\">\n  <PropertyGroup>\n    <TargetFramework>{profile.TargetFramework}</TargetFramework>\n    <RootNamespace>{request.RootNamespace}</RootNamespace>\n    <ImplicitUsings>enable</ImplicitUsings>\n    <Nullable>enable</Nullable>\n    <IsPackable>false</IsPackable>\n    <!-- The source .play model is authoritative, so reverse code-to-model extraction is redundant. -->\n    <CratisEmbeddedScreenplayEnabled>false</CratisEmbeddedScreenplayEnabled>\n    <IsTestProject Condition=\"'$(Configuration)' == 'Debug'\">true</IsTestProject>\n    <NoWarn Condition=\"'$(Configuration)' == 'Debug'\">$(NoWarn);CS7022</NoWarn>\n    <CratisProxiesOutputPath>$(MSBuildThisFileDirectory)</CratisProxiesOutputPath>\n    <CratisProxiesSegmentsToSkip>1</CratisProxiesSegmentsToSkip>\n    <CratisProxiesSkipOutputDeletion>true</CratisProxiesSkipOutputDeletion>\n    <CratisProxiesSkipCommandNameInRoute>true</CratisProxiesSkipCommandNameInRoute>\n    <CratisProxiesUseSourceFileAsOutputFile>true</CratisProxiesUseSourceFileAsOutputFile>\n  </PropertyGroup>\n  <ItemGroup>\n    <Content Remove=\"package.json\" />\n    <None Remove=\"package.json\" />\n  </ItemGroup>\n  <ItemGroup>\n    <PackageReference Include=\"Cratis\" Version=\"{profile.CratisPackageVersion}\" />\n    <PackageReference Include=\"Cratis.Arc.MongoDB\" Version=\"{profile.CratisArcMongoDBPackageVersion}\" />\n    <PackageReference Include=\"Cratis.Chronicle\" Version=\"{profile.CratisChroniclePackageVersion}\" />\n    <PackageReference Include=\"Cratis.Chronicle.AspNetCore\" Version=\"{profile.CratisChroniclePackageVersion}\" />\n  </ItemGroup>\n  <ItemGroup Condition=\"'$(Configuration)' == 'Debug'\">\n    <PackageReference Include=\"Cratis.Arc.Chronicle.Testing\" Version=\"{profile.CratisArcChronicleTestingPackageVersion}\" />\n    <PackageReference Include=\"Cratis.Chronicle.Testing\" Version=\"{profile.CratisChroniclePackageVersion}\" />\n    <PackageReference Include=\"Cratis.Specifications\" Version=\"{profile.CratisSpecificationsPackageVersion}\" />\n    <PackageReference Include=\"Cratis.Specifications.XUnit\" Version=\"{profile.CratisSpecificationsXUnitPackageVersion}\" />\n    <PackageReference Include=\"Microsoft.NET.Test.Sdk\" Version=\"{profile.MicrosoftNetTestSdkPackageVersion}\" />\n    <PackageReference Include=\"NSubstitute\" Version=\"{profile.NSubstitutePackageVersion}\" />\n    <PackageReference Include=\"xunit\" Version=\"{profile.XunitPackageVersion}\" />\n    <PackageReference Include=\"xunit.runner.visualstudio\" Version=\"{profile.XunitRunnerVisualStudioPackageVersion}\">\n      <PrivateAssets>all</PrivateAssets>\n      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>\n    </PackageReference>\n  </ItemGroup>\n  <Import Project=\"Customizations/Dependencies.props\" Condition=\"Exists('Customizations/Dependencies.props')\" />\n</Project>";
	}

	private static string Program(CratisBackendApplicationScaffoldRequest request)
	{
		return "// Copyright (c) Cratis. All rights reserved.\n// Licensed under the MIT license. See LICENSE file in the project root for full license information.\n\nusing Cratis.Arc.MongoDB;\n\nvar builder = WebApplication.CreateBuilder(args);\nbuilder.Services.AddHealthChecks();\nbuilder.AddCratis(\n    configureArcBuilder: arc => arc.WithMongoDB(),\n    configureChronicleBuilder: chronicle => chronicle.WithCamelCaseNamingPolicy());\n" + request.RootNamespace + ".GeneratedPolicies.Registration.Register(builder.Services);\nConfigureServices(builder);\n\nvar app = builder.Build();\napp.UseDefaultFiles();\napp.UseStaticFiles();\napp.UseCratis();\napp.MapHealthChecks(\"/healthz\");\napp.MapFallbackToFile(\"/index.html\");\nConfigureApplication(app);\n\nawait app.RunAsync();\n\n// Optional partial methods disappear when Customizations/Program.cs supplies no implementation.\npublic partial class Program\n{\n    static partial void ConfigureServices(WebApplicationBuilder builder);\n    static partial void ConfigureApplication(WebApplication app);\n}";
	}

	private static string PolicyRegistration(CratisBackendApplicationScaffoldRequest request)
	{
		return "// Copyright (c) Cratis. All rights reserved.\n// Licensed under the MIT license. See LICENSE file in the project root for full license information.\n\nusing Microsoft.Extensions.DependencyInjection;\n\nnamespace " + request.RootNamespace + ".GeneratedPolicies;\n\n/// <summary>\n/// Registers the policies generated from the Screenplay application.\n/// </summary>\npublic static partial class Registration\n{\n    /// <summary>\n    /// Registers generated policies when the application contains protected operations.\n    /// </summary>\n    /// <param name=\"services\">The application's services.</param>\n    public static void Register(IServiceCollection services) => RegisterGenerated(services);\n\n    static partial void RegisterGenerated(IServiceCollection services);\n}";
	}

	private static string AppSettings(CratisBackendApplicationScaffoldRequest request)
	{
		return $"{{\n  \"Cratis\": {{\n    \"Arc\": {{\n      \"GeneratedApis\": {{\n        \"RoutePrefix\": \"api\",\n        \"IncludeCommandNameInRoute\": false,\n        \"SegmentsToSkipForRoute\": 1\n      }}\n    }},\n    \"Chronicle\": {{\n      \"EventStore\": \"{request.ApplicationName}\",\n      \"ConnectionString\": \"chronicle://chronicle-dev-client:chronicle-dev-secret@localhost:35000\"\n    }},\n    \"MongoDB\": {{\n      \"Server\": \"mongodb://localhost:27017\",\n      \"Database\": \"{request.ApplicationName}\"\n    }}\n  }}\n}}";
	}

	private static string DockerCompose(CratisBackendApplicationScaffoldProfile profile)
	{
		return "services:\n  chronicle:\n    image: cratis/chronicle:" + profile.ChronicleImageVersion + "-development\n    ports:\n      - \"27017:27017\"\n      - \"35000:35000\"\n    volumes:\n      - chronicle-data:/data/db\n      - chronicle-config:/data/configdb\n\nvolumes:\n  chronicle-data:\n  chronicle-config:";
	}
}
You are not using the latest version of the tool, please update.
Latest version is '11.1.0.9782' (yours is '9.1.0.7988')
