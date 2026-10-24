// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Cratis.Scaffolding;

public sealed partial class CratisBackendApplicationScaffold
{
    // All specification renderers emit slice-local when_*.cs files, including query/projection companions.
    // They emit no separate given helpers or shared specification support files.
    const string SpecificationGlob = "**/when_*.cs";

    static string DirectoryBuildProps(CratisBackendApplicationScaffoldProfile profile) =>
        $$"""
        <Project>
          <PropertyGroup>
            <TargetFramework>{{profile.TargetFramework}}</TargetFramework>
            <LangVersion>latest</LangVersion>
            <Nullable>enable</Nullable>
            <ImplicitUsings>enable</ImplicitUsings>
            <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
            <!-- Source mapping and audit availability/advisories depend on the restore environment; keep them visible as warnings. -->
            <WarningsNotAsErrors>$(WarningsNotAsErrors);NU1507;NU1900;NU1901;NU1902;NU1903;NU1904</WarningsNotAsErrors>
            <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
            <!-- Use this application's analyzer rules, not configuration discovered in ancestor repositories. -->
            <DiscoverEditorConfigFiles>false</DiscoverEditorConfigFiles>
            <DiscoverGlobalAnalyzerConfigFiles>false</DiscoverGlobalAnalyzerConfigFiles>
            <!-- The source .play model is authoritative, so reverse code-to-model extraction is redundant. -->
            <CratisEmbeddedScreenplayEnabled>false</CratisEmbeddedScreenplayEnabled>
          </PropertyGroup>
          <ItemGroup>
            <EditorConfigFiles Include="$(MSBuildThisFileDirectory).editorconfig" />
          </ItemGroup>
        </Project>
        """;

    static string DirectoryBuildTargets() => "<Project />";

    static string DirectoryPackagesProps(CratisBackendApplicationScaffoldProfile profile) =>
        $$"""
        <Project>
          <PropertyGroup>
            <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
            <CentralPackageTransitivePinningEnabled>true</CentralPackageTransitivePinningEnabled>
          </PropertyGroup>
          <ItemGroup>
            <PackageVersion Include="Cratis.Screenplay.Matching" Version="{{ScaffoldPackageVersions.ScreenplayMatching}}" />
            <PackageVersion Include="Cratis" Version="{{profile.CratisPackageVersion}}" />
            <PackageVersion Include="Cratis.Arc.Chronicle.Testing" Version="{{profile.CratisArcChronicleTestingPackageVersion}}" />
            <PackageVersion Include="Cratis.Arc.MongoDB" Version="{{profile.CratisArcMongoDBPackageVersion}}" />
            <PackageVersion Include="Cratis.Chronicle" Version="{{profile.CratisChroniclePackageVersion}}" />
            <PackageVersion Include="Cratis.Chronicle.AspNetCore" Version="{{profile.CratisChroniclePackageVersion}}" />
            <PackageVersion Include="Cratis.Chronicle.Testing" Version="{{profile.CratisChroniclePackageVersion}}" />
            <PackageVersion Include="Cratis.Specifications" Version="{{profile.CratisSpecificationsPackageVersion}}" />
            <PackageVersion Include="Cratis.Specifications.XUnit" Version="{{profile.CratisSpecificationsXUnitPackageVersion}}" />
            <PackageVersion Include="Microsoft.NET.Test.Sdk" Version="{{profile.MicrosoftNetTestSdkPackageVersion}}" />
            <PackageVersion Include="NSubstitute" Version="{{profile.NSubstitutePackageVersion}}" />
            <PackageVersion Include="xunit" Version="{{profile.XunitPackageVersion}}" />
            <PackageVersion Include="xunit.runner.visualstudio" Version="{{profile.XunitRunnerVisualStudioPackageVersion}}" />
          </ItemGroup>
        </Project>
        """;

    static string Solution(CratisBackendApplicationScaffoldRequest request) =>
        $$"""
        <Solution>
          <Project Path="{{request.ProjectName}}.csproj" />
          <Project Path="{{request.ProjectName}}.Specs/{{request.ProjectName}}.Specs.csproj" />
        </Solution>
        """;

    static string Project(CratisBackendApplicationScaffoldRequest request) =>
        $$"""
        <Project Sdk="Microsoft.NET.Sdk.Web">
          <PropertyGroup>
            <RootNamespace>{{request.RootNamespace}}</RootNamespace>
            <IsPackable>false</IsPackable>
            <CratisProxiesOutputPath>$(MSBuildThisFileDirectory)</CratisProxiesOutputPath>
            <CratisProxiesSegmentsToSkip>1</CratisProxiesSegmentsToSkip>
            <CratisProxiesSkipOutputDeletion>true</CratisProxiesSkipOutputDeletion>
            <CratisProxiesSkipCommandNameInRoute>true</CratisProxiesSkipCommandNameInRoute>
            <CratisProxiesUseSourceFileAsOutputFile>true</CratisProxiesUseSourceFileAsOutputFile>
          </PropertyGroup>
          <ItemGroup>
            <InternalsVisibleTo Include="{{request.ProjectName}}.Specs" />
            <Compile Remove="{{SpecificationGlob}};{{request.ProjectName}}.Specs/**" />
            <Content Remove="package.json;{{request.ProjectName}}.Specs/**" />
            <None Remove="package.json;{{request.ProjectName}}.Specs/**" />
            <EmbeddedResource Remove="{{request.ProjectName}}.Specs/**" />
          </ItemGroup>
          <ItemGroup>
            <PackageReference Include="Cratis.Screenplay.Matching" />
            <PackageReference Include="Cratis" />
            <PackageReference Include="Cratis.Arc.MongoDB" />
            <PackageReference Include="Cratis.Chronicle" />
            <PackageReference Include="Cratis.Chronicle.AspNetCore" />
          </ItemGroup>
          <Import Project="Customizations/Dependencies.props" Condition="Exists('Customizations/Dependencies.props')" />
        </Project>
        """;

    static string SpecificationsProject(CratisBackendApplicationScaffoldRequest request) =>
        $$"""
        <Project Sdk="Microsoft.NET.Sdk">
          <PropertyGroup>
            <RootNamespace>{{request.RootNamespace}}.Specs</RootNamespace>
            <IsTestProject>true</IsTestProject>
            <IsPackable>false</IsPackable>
          </PropertyGroup>
          <ItemGroup>
            <ProjectReference Include="../{{request.ProjectName}}.csproj" />
            <Compile Include="../{{SpecificationGlob}}" Exclude="../{{request.ProjectName}}.Specs/**;../**/bin/**;../**/obj/**;../node_modules/**" Link="%(RecursiveDir)%(Filename)%(Extension)" />
          </ItemGroup>
          <ItemGroup>
            <PackageReference Include="Cratis.Arc.Chronicle.Testing" />
            <PackageReference Include="Cratis.Chronicle.Testing" />
            <PackageReference Include="Cratis.Specifications" />
            <PackageReference Include="Cratis.Specifications.XUnit" />
            <PackageReference Include="Microsoft.NET.Test.Sdk" />
            <PackageReference Include="NSubstitute" />
            <PackageReference Include="xunit" />
            <PackageReference Include="xunit.runner.visualstudio">
              <PrivateAssets>all</PrivateAssets>
              <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
            </PackageReference>
          </ItemGroup>
        </Project>
        """;
}
