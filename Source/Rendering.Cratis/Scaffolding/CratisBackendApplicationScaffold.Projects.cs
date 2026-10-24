// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Cratis.Scaffolding;

public sealed partial class CratisBackendApplicationScaffold
{
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

    static string DirectoryBuildTargets() =>
        """
        <Project>
          <!-- Default Compile items exist here, after the SDK's props have evaluated. -->
          <ItemGroup Condition="'$(Configuration)' == 'Release'">
            <Compile Remove="**/for_*/**/*.cs;**/when_*.cs;**/when_*/**/*.cs;**/given/**/*.cs" />
          </ItemGroup>
        </Project>
        """;

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
            <PackageVersion Include="Cratis.Fundamentals" Version="{{profile.CratisFundamentalsPackageVersion}}" />
            <PackageVersion Include="Cratis.Arc.Chronicle.Testing" Version="{{profile.CratisArcChronicleTestingPackageVersion}}" />
            <PackageVersion Include="Cratis.Arc.MongoDB" Version="{{profile.CratisArcMongoDBPackageVersion}}" />
            <PackageVersion Include="Cratis.Chronicle" Version="{{profile.CratisChroniclePackageVersion}}" />
            <PackageVersion Include="Cratis.Chronicle.AspNetCore" Version="{{profile.CratisChroniclePackageVersion}}" />
            <PackageVersion Include="Cratis.Chronicle.Testing" Version="{{profile.CratisChroniclePackageVersion}}" />
            <PackageVersion Include="Cratis.Specifications" Version="{{profile.CratisSpecificationsPackageVersion}}" />
            <PackageVersion Include="Cratis.Specifications.XUnit.v3" Version="{{profile.CratisSpecificationsXUnitV3PackageVersion}}" />
            <PackageVersion Include="NSubstitute" Version="{{profile.NSubstitutePackageVersion}}" />
            <PackageVersion Include="xunit.v3" Version="{{profile.XunitV3PackageVersion}}" />
          </ItemGroup>
        </Project>
        """;

    static string Solution(CratisBackendApplicationScaffoldRequest request) =>
        $$"""
        <Solution>
          <Project Path="{{request.ProjectName}}.csproj" />
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
            <DefaultItemExcludes>$(DefaultItemExcludes);**/bin/**;**/obj/**;node_modules/**</DefaultItemExcludes>
          </PropertyGroup>
          <PropertyGroup Condition="'$(Configuration)' == 'Debug'">
            <XunitAutoGeneratedEntryPoint>false</XunitAutoGeneratedEntryPoint>
            <CratisSpecificationsReadableNames>false</CratisSpecificationsReadableNames>
          </PropertyGroup>
          <ItemGroup>
            <Content Remove="package.json" />
            <None Remove="package.json" />
          </ItemGroup>
          <ItemGroup>
            <PackageReference Include="Cratis.Screenplay.Matching" />
            <PackageReference Include="Cratis" />
            <PackageReference Include="Cratis.Arc.MongoDB" />
            <PackageReference Include="Cratis.Chronicle" />
            <PackageReference Include="Cratis.Chronicle.AspNetCore" />
          </ItemGroup>
          <ItemGroup Condition="'$(Configuration)' == 'Debug'">
            <PackageReference Include="Cratis.Arc.Chronicle.Testing" />
            <PackageReference Include="Cratis.Chronicle.Testing" />
            <PackageReference Include="Cratis.Specifications" />
            <PackageReference Include="Cratis.Specifications.XUnit.v3" />
            <PackageReference Include="NSubstitute" />
            <PackageReference Include="xunit.v3" />
          </ItemGroup>
          <Import Project="Customizations/Dependencies.props" Condition="Exists('Customizations/Dependencies.props')" />
        </Project>
        """;
}
