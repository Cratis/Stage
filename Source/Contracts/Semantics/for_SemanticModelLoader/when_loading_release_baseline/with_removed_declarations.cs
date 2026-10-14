// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Semantics.for_SemanticModelLoader.when_loading_release_baseline;

public class with_removed_declarations : given.a_release_baseline_tree
{
    void Establish() => File.WriteAllText(Path.Combine(_root, "source.play"), Source.Replace("      event Removed", "", StringComparison.Ordinal));
    async Task Because() => _loaded = await SemanticModelLoader.LoadAsync(_root, [], ".screenplay/identities.json", "RecordsApp");
    [Fact] void should_preserve_the_compiler_refusal_of_stale_assignments() => _loaded.Success.ShouldBeFalse();
    [Fact] void should_report_a_compilation_failure() => _loaded.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == "STAGE-PLAN-003");
    [Fact] void should_not_rewrite_the_identity_state() => File.ReadAllBytes(Path.Combine(_root, ".screenplay", "identities.json")).ShouldEqual(_catalogBytes);
}
#endif
