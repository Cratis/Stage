// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Kits.for_KitLoader;

public class when_binding_a_manifest : Specification
{
    [Theory]
    [InlineData("stage-target/v1.0", "stage-target/v2.0", KitCodes.UnsupportedApi)]
    [InlineData("stage-target/v1.0", "stage-target/v1", KitCodes.UnsupportedApi)]
    [InlineData("stage-target/v1.0", "stage-target/v1.1", KitCodes.NewerApi)]
    [InlineData("java@1.0.0", "java@^1.0.0", KitCodes.InvalidLanguagePack)]
    [InlineData("java@1.0.0", "java@1.0.1", KitCodes.MissingLanguagePack)]
    [InlineData("model/v1.0", "missing/v1.0", KitCodes.MissingContract)]
    [InlineData("model/v1.0", "model/v2.0", KitCodes.MissingContract)]
    [InlineData("model/v1.0", "model/v1.2", KitCodes.NewerContract)]
    [InlineData("\"diagnostics\":[]", "\"diagnostics\":[{\"code\":\"STAGE-MINE\",\"summary\":\"oops\"}]", KitCodes.InvalidDeclaration)]
    [InlineData("\"id\":\"axon\"", "\"id\":\"Axon\"", KitCodes.InvalidDeclaration)]
    [InlineData("\"version\":\"1.0.0\"", "\"version\":\"1.*\"", KitCodes.InvalidDeclaration)]
    [InlineData("\"id\":\"main\"", "\"id\":\"\"", KitCodes.InvalidManifest)]
    [InlineData("\"partials\":[]", "\"partials\":[{\"name\":\"p\",\"template\":\"templates/main.sbn\"}]", KitCodes.DuplicateDeclaration)]
    void should_refuse_incompatible_declarations(string original, string replacement, string code) => given.kit.Load(given.kit.Manifest.Replace(original, replacement, StringComparison.Ordinal)).Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain(code);

    [Fact] void should_load_an_axon_java_kit() => given.kit.Load(given.kit.Manifest).Kit!.Manifest.Target.Id.ShouldEqual("axon");
    [Fact] void should_accept_an_older_contract_minor() => given.kit.Load(given.kit.Manifest).Succeeded.ShouldBeTrue();
    [Fact] void should_preserve_opaque_output_expressions() => given.kit.Load(given.kit.Manifest).Kit!.Manifest.Outputs.Single().Path.ShouldEqual("{{ name }}.java");
    [Fact] void should_refuse_duplicate_output_ids() => given.kit.Load(given.kit.Change("outputs", JsonNode.Parse("""[{"id":"main","template":"a","path":"a"},{"id":"main","template":"b","path":"b"}]"""))).Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain(KitCodes.DuplicateDeclaration);
}
