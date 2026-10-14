// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Semantics.for_SemanticModelLoader.when_loading_release_baseline;

public class with_an_unknown_identity_schema : given.a_release_baseline_tree
{
    void Establish() => File.WriteAllBytes(Path.Combine(_root, ".screenplay", "identities.json"), "{\"schema\":\"unknown\"}"u8.ToArray());
    async Task Because() => _loaded = await SemanticModelLoader.LoadAsync(_root, [], ".screenplay/identities.json", "RecordsApp");
    [Fact] void should_not_load_the_model() => _loaded.Success.ShouldBeFalse();
    [Fact] void should_report_an_invalid_catalog() => _loaded.Diagnostics.Single().Code.ShouldEqual("STAGE-PLAN-006");
    [Fact] void should_preserve_the_bare_catalog_diagnostic() => _loaded.Diagnostics.Single().Message.ShouldEqual(Catch.Exception(() => SemanticIdentityCatalogSerializer.Deserialize("{\"schema\":\"unknown\"}"u8.ToArray())).Message);
}
#endif
