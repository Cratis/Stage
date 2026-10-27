// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Kits.for_KitLoader;

public class when_reading_manifests : Specification
{
    [Theory]
    [InlineData("{", KitCodes.InvalidManifest)]
    [InlineData("null", KitCodes.InvalidManifest)]
    [InlineData("[]", KitCodes.InvalidManifest)]
    [InlineData("{}", KitCodes.InvalidManifest)]
    [InlineData("{\"apiVersion\":\"stage-target/v1.0\",\"apiVersion\":\"stage-target/v1.0\"}", KitCodes.InvalidManifest)]
    [InlineData("{/* comment */}", KitCodes.InvalidManifest)]
    [InlineData("{\"target\":{},}", KitCodes.InvalidManifest)]
    void should_refuse_invalid_json(string manifest, string code) => given.kit.Load(manifest).Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain(code);

    [Theory]
    [InlineData("target")]
    [InlineData("languagePacks")]
    [InlineData("outputs")]
    [InlineData("apiVersion")]
    void should_refuse_required_nulls(string property) => given.kit.Load(given.kit.Change(property, null)).Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain(KitCodes.InvalidManifest);

    [Fact] void should_refuse_null_array_elements() => given.kit.Load(given.kit.Change("outputs", new JsonArray((JsonNode?)null))).Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain(KitCodes.InvalidManifest);
    [Fact] void should_report_unknown_root_member_path() => given.kit.Load(given.kit.Manifest.Replace("\"partials\":[]", "\"surprise\":true,\"partials\":[]", StringComparison.Ordinal)).Diagnostics.Single().Path.ShouldEqual("$.surprise");
    [Fact] void should_report_unknown_nested_member_path() => given.kit.Load(given.kit.Manifest.Replace("\"id\":\"main\"", "\"id\":\"main\",\"extra\":0", StringComparison.Ordinal)).Diagnostics.Single().ShouldEqual(new KitDiagnostic(KitCodes.UnknownMember, "Unknown manifest member.", "$.outputs[0].extra"));
}
