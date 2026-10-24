// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Xml.Linq;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisBackendApplicationScaffold.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisBackendApplicationScaffold;

public class when_separating_generated_specifications : a_current_scaffold
{
    [Fact] void should_keep_one_backend_project() => _first.Select(PathOf).Where(path => path.EndsWith(".csproj", StringComparison.Ordinal)).ShouldContainOnly("MyApp.csproj");
    [Fact] void should_include_inline_specifications_in_debug() => CompileRemovals().ShouldBeEmpty();
    [Fact] void should_remove_specifications_and_contexts_in_release() => ReleaseCompileRemovals().ShouldContainOnly("**/for_*/**/*.cs", "**/when_*.cs", "**/when_*/**/*.cs", "**/given/**/*.cs");
    [Fact] void should_keep_proxies_beside_the_slice() => Content("MyApp.csproj").ShouldContain("<CratisProxiesOutputPath>$(MSBuildThisFileDirectory)</CratisProxiesOutputPath>");
    [Fact] void should_keep_the_web_sdk_for_default_static_content() => XDocument.Parse(Content("MyApp.csproj")).Root!.Attribute("Sdk")!.Value.ShouldEqual("Microsoft.NET.Sdk.Web");
    [Fact] void should_keep_the_root_namespace() => Content("MyApp.csproj").ShouldContain("<RootNamespace>MyApp</RootNamespace>");
    [Fact] void should_guard_the_native_runner_by_debug() => Content("Program.cs").ShouldContain("#if DEBUG\nif (args.Length > 0 && args[0] == \"--specifications\")");
    [Fact] void should_document_the_native_inline_runner() => Content("README.md").ShouldContain("dotnet run --project MyApp.csproj -c Debug -- --specifications");
    [Fact] void should_document_test_free_release() => Content("README.md").ShouldContain("Release excludes the specification\nsources and test dependencies.");
    [Fact] void should_exclude_nested_build_outputs_from_default_items() => Content("MyApp.csproj").ShouldContain("<DefaultItemExcludes>$(DefaultItemExcludes);**/bin/**;**/obj/**;node_modules/**</DefaultItemExcludes>");

    string[] CompileRemovals() => [.. XDocument.Parse(Content("MyApp.csproj")).Descendants("Compile")
        .Select(element => element.Attribute("Remove")?.Value).Where(value => value is not null).Cast<string>()];

    string[] ReleaseCompileRemovals() => XDocument.Parse(Content("Directory.Build.targets"))
        .Descendants("ItemGroup").Single(group => group.Attribute("Condition")?.Value == "'$(Configuration)' == 'Release'")
        .Elements("Compile").Single().Attribute("Remove")!.Value.Split(';');
}
