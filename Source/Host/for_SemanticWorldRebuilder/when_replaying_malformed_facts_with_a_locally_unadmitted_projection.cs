// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Semantics;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticWorldRebuilder;

public class when_replaying_malformed_facts_with_a_locally_unadmitted_projection : given.a_locally_unadmitted_projection
{
    SemanticRejected _original = null!;
    SemanticRejected _isolated = null!;

    void Because()
    {
        var malformed = _storedFact with { Values = [] };
        var evaluator = new SemanticEvaluator();
        _original = Assert.IsType<SemanticRejected>(evaluator.EstablishWorld(_plan, [malformed]));
        _isolated = Assert.IsType<SemanticRejected>(evaluator.EstablishWorld(SemanticProjectionAdmission.ForReplay(_plan), [malformed]));
    }

    [Fact] void should_keep_the_contract_rejection() => Assert.Equal(_original.Category, _isolated.Category);
    [Fact] void should_keep_the_exact_rejection_details() => Assert.Equal(_original.Details, _isolated.Details);
    [Fact] void should_not_accept_any_malformed_facts() => Assert.Empty(_isolated.World.Facts);
}
