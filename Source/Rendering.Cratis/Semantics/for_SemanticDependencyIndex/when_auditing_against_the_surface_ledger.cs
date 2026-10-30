// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticDependencyIndex;

public class when_auditing_against_the_surface_ledger : Specification
{
    string[] _unclassified = [];

    void Because() => _unclassified = [.. SemanticSurfaceLedger.Entries.Keys
        .Where(member => SemanticDependencySurface.Classify(member) is null)
        .Order(StringComparer.Ordinal)];

    [Fact] void should_classify_every_audited_surface_member() => Assert.True(_unclassified.Length == 0, string.Join(Environment.NewLine, _unclassified));
    [Fact] void should_not_classify_a_fabricated_event_reference() => SemanticDependencySurface.Classify("SemanticEventContract.FutureReference").ShouldBeNull();
}
