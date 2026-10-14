// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Semantics.for_SemanticModelLoader.when_loading_release_baseline;

public class with_a_malformed_wrapper : given.a_release_baseline_tree
{
    void Establish() => File.WriteAllText(Path.Combine(_root, ".screenplay", "identities.json"), "{\"schema\":\"cratis.screenplay.mcp-identities\",\"schemaVersion\":1}");
    async Task Because() => _loaded = await SemanticModelLoader.LoadAsync(_root, [], ".screenplay/identities.json", "RecordsApp");
    [Fact] void should_not_load_the_model() => _loaded.Success.ShouldBeFalse();
    [Fact] void should_report_an_invalid_catalog() => _loaded.Diagnostics.Single().Code.ShouldEqual("STAGE-PLAN-006");
    [Fact] void should_name_the_catalog_path() => _loaded.Diagnostics.Single().Source.ShouldEqual(".screenplay/identities.json");
}
#endif
