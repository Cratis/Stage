// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Semantics.for_SemanticModelLoader;

public class when_loading_without_throwing : Specification
{
    SemanticModelLoadResult _result = null!;
    async Task Because() => _result = await SemanticModelLoader.LoadAsync(Directory.GetCurrentDirectory(), ["absent-source.play"], null, "Shop");
    [Fact] void should_report_a_missing_source() => _result.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain("STAGE-PLAN-001");
    [Fact] void should_not_claim_loading_succeeded() => _result.Success.ShouldBeFalse();
    [Fact] void should_have_no_model() => _result.Loaded.ShouldBeNull();
}
#endif
