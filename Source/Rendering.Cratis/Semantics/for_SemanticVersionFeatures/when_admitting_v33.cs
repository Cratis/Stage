// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticVersionFeatures;

public class when_admitting_v33 : Specification
{
    [Theory]
    [InlineData(33, 33, 0, true)]
    [InlineData(35, 35, 0, false)]
    [InlineData(32, 33, 0, false)]
    [InlineData(33, 32, 0, false)]
    [InlineData(33, 33, 1, false)]
    [InlineData(35, 35, 0, false)]
    public void should_admit_only_the_audited_exact_pair(int languageMajor, int semanticMajor, int minor, bool supported)
    {
        var model = SemanticModelSerializer.Deserialize(EsmGoldenVectors.Get(33).Bytes.AsSpan());
        typeof(ExecutableSemanticModel).GetField("<LanguageVersion>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(model, new LanguageVersion((uint)languageMajor, (uint)minor));
        typeof(ExecutableSemanticModel).GetField("<SemanticVersion>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(model, new SemanticVersion((uint)semanticMajor, (uint)minor));
        SemanticVersionFeatures.Supports(model).ShouldEqual(supported);
    }

    [Fact]
    void should_keep_typed_identity_validation_for_v33()
    {
        var model = SemanticModelSerializer.Deserialize(EsmGoldenVectors.Get(33).Bytes.AsSpan());
        var slice = SemanticVersionFeatures.Slices(model.Application).Single(slice => slice.Captures.Length > 0);
        var capture = slice.Captures.Single();
        var events = slice.Events.ToDictionary(contract => contract.Id);
        SemanticCaptureIdentitySupport.Check(capture, model, events).ShouldBeNull();
        var mismatched = capture with { Appends = [capture.Appends.Single() with { EventSourceType = SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text) }] };
        SemanticCaptureIdentitySupport.Check(mismatched, model, events).ShouldContain("will not coerce");
    }
}
