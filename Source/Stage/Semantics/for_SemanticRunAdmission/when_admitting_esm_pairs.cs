// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Semantics.for_SemanticRunAdmission.given;
using Xunit;

namespace Cratis.Stage.Semantics.for_SemanticRunAdmission;

public class when_admitting_esm_pairs : Specification
{
    ExecutableSemanticModel _audited = null!;
    ExecutableSemanticModel _newer = null!;
    ExecutableSemanticModel _future = null!;
    ExecutableSemanticModel _unaudited = null!;
    ExecutableSemanticModel _v34 = null!;

    void Establish()
    {
        _audited = a_model_at_an_esm_pair.From(32, 0);
        _newer = a_model_at_an_esm_pair.From(32, 1);
        _future = a_model_at_an_esm_pair.From(35, 0);
        _unaudited = a_model_at_an_esm_pair.From(32, 1, application => application with { Policies = [new("Sensitive policy", new SemanticAnonymousCondition())] });
        _v34 = a_model_at_an_esm_pair.From(34, 0);
    }

    [Fact] void should_run_an_audited_pair() => SemanticRunAdmission.SupportsRun(_audited).ShouldBeTrue();
    [Fact] void should_run_a_newer_minor_within_the_range() => SemanticRunAdmission.SupportsRun(_newer).ShouldBeTrue();
    [Fact] void should_keep_running_v34() => SemanticRunAdmission.SupportsRun(_v34).ShouldBeTrue();
    [Fact] void should_not_run_a_future_major() => SemanticRunAdmission.SupportsRun(_future).ShouldBeFalse();
    [Fact] void should_admit_a_newer_minor_without_unaudited_members() => SemanticRunAdmission.ModelFeatures(_newer).ShouldBeEmpty();
    [Fact] void should_refuse_a_future_major_naming_the_range() => SemanticRunAdmission.ModelFeatures(_future).Single().Details.Contains(">=32.0 <33", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_refuse_the_owner_of_an_unaudited_member() => SemanticRunAdmission.ModelFeatures(_unaudited).Single().Artifact.ShouldEqual(_unaudited.Application.Id.ToString());
    [Fact] void should_name_the_member_without_the_declaration() => SemanticRunAdmission.ModelFeatures(_unaudited).Single().Details.Contains("Sensitive policy", StringComparison.Ordinal).ShouldBeFalse();
}
