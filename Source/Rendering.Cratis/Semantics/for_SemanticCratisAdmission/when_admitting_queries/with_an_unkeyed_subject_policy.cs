// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticCratisAdmission.when_admitting_queries;

public class with_an_unkeyed_subject_policy : given.a_query_shape
{
    void Establish()
    {
        Configure(SemanticQueryCardinality.Many, SemanticQueryDelivery.Live, false);
        _model = ExecutableSemanticModel.Create(_model.LanguageVersion, _model.SemanticVersion, _model.Application with { Policies = [new("Owns", new SemanticClaimCondition("owner", SemanticClaimTargetKind.Subject, null))] });
        _query = _query with { Authorization = new SemanticPolicyReference("Owns") };
        _slice = _slice with { Queries = [_query] };
    }

    void Because() => Evaluate();

    [Fact] void should_refuse_the_missing_subject() => _diagnostics.Select(_ => _.Code).ShouldContainOnly("STAGE-ESM-015");
}
