// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Conformance.Specs.for_CanonicalCorpusConformance.given;
using Xunit;

namespace Cratis.Stage.Conformance.Specs.for_CanonicalCorpusConformance;

public class when_recording_a_rendered_compile_error : Specification
{
    readonly Dictionary<refusal_fingerprint, string> _codes = [];

    void Because() => conformance_outcomes.RecordCompilation("Vector", [new("CS0246", "Features/Bad.cs", 3, "Missing type"), new("CS0103", "Features/Other.cs", 4, "Missing name")], _codes);

    [Fact] void should_record_only_the_first_error() => _codes.Count.ShouldEqual(1);
    [Fact] void should_refuse_the_cell() => _codes.ShouldNotBeEmpty();
    [Fact] void should_record_the_code() => _codes.Keys.Single().Code.ShouldEqual("CS0246");
    [Fact] void should_record_the_path() => _codes.Keys.Single().Artifact.ShouldEqual("Features/Bad.cs");
    [Fact] void should_record_the_compile_kind() => _codes.Keys.Single().Kind.ShouldEqual("compile");
    [Fact] void should_keep_the_line_and_message_in_details() => _codes.Values.Single().ShouldContain("Features/Bad.cs(3): CS0246 Missing type (+1 more)");
    [Fact] void should_name_the_vector_in_details() => _codes.Values.Single().StartsWith("Vector: ", StringComparison.Ordinal).ShouldBeTrue();
}
#endif
