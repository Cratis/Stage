// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Semantics.for_SemanticModelLoader.when_loading_release_baseline;

public class with_pending_recovery : given.a_release_baseline_tree
{
    void Establish() => File.WriteAllText(Path.Combine(_root, ".screenplay", "pending.json"), "{}");
    async Task Because() => _loaded = await SemanticModelLoader.LoadAsync(_root, [], ".screenplay/identities.json", "RecordsApp");
    [Fact] void should_refuse_the_uncertain_root() => _loaded.Success.ShouldBeFalse();
    [Fact] void should_report_the_typed_failure() => _loaded.Diagnostics.Single().Code.ShouldEqual("STAGE-BASELINE-004");
    [Fact] void should_keep_the_pending_operation() => File.Exists(Path.Combine(_root, ".screenplay", "pending.json")).ShouldBeTrue();
}
#endif
