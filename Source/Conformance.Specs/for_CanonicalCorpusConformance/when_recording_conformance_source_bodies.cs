// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Conformance.Specs.for_CanonicalCorpusConformance.given;
using Xunit;

namespace Cratis.Stage.Conformance.Specs.for_CanonicalCorpusConformance;

public class when_recording_conformance_source_bodies : Specification
{
    [Fact]
    void should_keep_the_anonymous_policy_source_form_equivalent_to_the_vector()
    {
        var vector = canonical_corpus_vectors.All.Single(vector => vector.Name == "AnonymousPoliciesCorpus.V31");
        var (loaded, problem) = conformance_source_bodies.Compile(vector);
        problem.ShouldBeNull();
        loaded.ShouldNotBeNull();
        loaded.Model.Revision.ShouldEqual(vector.Model.Revision);
    }
}
