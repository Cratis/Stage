// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticVersionFeatures;

public class when_admitting_v20 : Specification
{
    [Theory]
    [InlineData(20, 0, 20, 0, true)]
    [InlineData(21, 0, 21, 0, true)]
    [InlineData(19, 0, 20, 0, false)]
    [InlineData(20, 0, 19, 0, false)]
    [InlineData(20, 1, 20, 1, false)]
    public void should_admit_only_released_exact_pairs(int languageMajor, int languageMinor, int semanticMajor, int semanticMinor, bool supported)
    {
        var model = SemanticModelSerializer.Deserialize(GeneratedConceptRulesCorpus.V20.EsmBytes.AsSpan());
        typeof(ExecutableSemanticModel).GetField("<LanguageVersion>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(model, new LanguageVersion((uint)languageMajor, (uint)languageMinor));
        typeof(ExecutableSemanticModel).GetField("<SemanticVersion>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(model, new SemanticVersion((uint)semanticMajor, (uint)semanticMinor));
        SemanticVersionFeatures.Supports(model).ShouldEqual(supported);
    }

    [Fact]
    public void should_preserve_the_v20_golden_bytes()
    {
        var vector = EsmGoldenVectors.Get(20);
        var model = SemanticModelSerializer.Deserialize(vector.Bytes.AsSpan());
        SemanticVersionFeatures.Supports(model).ShouldBeTrue();
        Assert.Equal([.. vector.Bytes], SemanticModelSerializer.Serialize(model));
    }
}
