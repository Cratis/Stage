// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticVersionFeatures;

public class when_admitting_v32 : Specification
{
    [Theory]
    [InlineData(32, 0, 32, 0, true)]
    [InlineData(34, 0, 34, 0, false)]
    [InlineData(32, 0, 31, 0, false)]
    [InlineData(31, 0, 32, 0, false)]
    [InlineData(32, 1, 32, 1, false)]
    [InlineData(33, 0, 33, 0, true)]
    public void should_admit_only_the_audited_exact_pair(int languageMajor, int languageMinor, int semanticMajor, int semanticMinor, bool supported)
    {
        var model = SemanticModelSerializer.Deserialize(EsmGoldenVectors.Get(32).Bytes.AsSpan());
        typeof(ExecutableSemanticModel).GetField("<LanguageVersion>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(model, new LanguageVersion((uint)languageMajor, (uint)languageMinor));
        typeof(ExecutableSemanticModel).GetField("<SemanticVersion>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(model, new SemanticVersion((uint)semanticMajor, (uint)semanticMinor));
        SemanticVersionFeatures.Supports(model).ShouldEqual(supported);
    }

    [Fact]
    void should_retain_the_typed_capture_source_from_the_frozen_vector()
    {
        var model = SemanticModelSerializer.Deserialize(EsmGoldenVectors.Get(32).Bytes.AsSpan());
        var capture = SemanticVersionFeatures.Slices(model.Application).SelectMany(slice => slice.Captures).Single();
        capture.Appends.Single().EventSourceType.ShouldEqual(SemanticTypeReference.ForConcept(model.Application.Concepts.Single().Id));
        SemanticVersionFeatures.Supports(model).ShouldBeTrue();
    }
}
