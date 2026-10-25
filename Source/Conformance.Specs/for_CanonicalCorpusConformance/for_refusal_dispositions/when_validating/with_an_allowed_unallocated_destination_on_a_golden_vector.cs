// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Conformance.Specs.for_CanonicalCorpusConformance.for_refusal_dispositions.when_validating;

public class with_an_allowed_unallocated_destination_on_a_golden_vector : Specification
{
    conformance_record _record = null!;

    void Establish() => _record = refusal_dispositions.Apply(new("EsmGoldenVectors.V11", "frozen", "11.0", "11.0", conformance_surface.Renderer, conformance_outcome.ExpectedRefusal, [new("STAGE-ESM-053", "event production", "sem1:000000000000000000000000000000000000000000000000000000000000277e")], []));

    [Fact] void should_classify_the_refusal_as_by_design() => _record.Disposition.ShouldEqual(refusal_disposition.ByDesign);
    [Fact] void should_have_no_unclassified_refusal() => refusal_dispositions.UnclassifiedRefusals([_record]).ShouldBeEmpty();
    [Fact] void should_not_allow_the_same_code_on_another_artifact() => refusal_dispositions.Validate([_record with { Diagnostics = [new("STAGE-ESM-053", "event production", "another-command")] }]).ShouldNotBeEmpty();
    [Fact] void should_not_allow_the_same_code_on_another_vector() => refusal_dispositions.Validate([_record with { Vector = "OtherVector" }]).ShouldNotBeEmpty();
    [Fact] void should_not_allow_the_same_code_on_the_host_surface() => refusal_dispositions.Validate([_record with { Surface = conformance_surface.Host }]).ShouldNotBeEmpty();
}
