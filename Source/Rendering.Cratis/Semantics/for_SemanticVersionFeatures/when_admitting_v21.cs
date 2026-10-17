// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticVersionFeatures;

public class when_admitting_v21 : Specification
{
    [Theory]
    [InlineData(21, 0, 21, 0, true)]
    [InlineData(21, 0, 22, 0, false)]
    [InlineData(20, 0, 21, 0, false)]
    [InlineData(21, 0, 20, 0, false)]
    [InlineData(21, 1, 21, 1, false)]
    public void should_admit_only_released_exact_pairs(int languageMajor, int languageMinor, int semanticMajor, int semanticMinor, bool supported)
    {
        var model = SemanticModelSerializer.Deserialize(EsmGoldenVectors.Get(21).Bytes.AsSpan());
        typeof(ExecutableSemanticModel).GetField("<LanguageVersion>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(model, new LanguageVersion((uint)languageMajor, (uint)languageMinor));
        typeof(ExecutableSemanticModel).GetField("<SemanticVersion>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(model, new SemanticVersion((uint)semanticMajor, (uint)semanticMinor));
        SemanticVersionFeatures.Supports(model).ShouldEqual(supported);
    }

    [Fact]
    void should_preserve_every_previous_golden_vector()
    {
        foreach (var version in Enumerable.Range(1, 20))
        {
            var vector = EsmGoldenVectors.Get(version);
            var model = SemanticModelSerializer.Deserialize(vector.Bytes.AsSpan());
            SemanticVersionFeatures.Supports(model).ShouldBeTrue();
            Assert.Equal([.. vector.Bytes], SemanticModelSerializer.Serialize(model));
        }
    }
}
