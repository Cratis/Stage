// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticVersionFeatures;

public class when_admitting_v19 : Specification
{
    [Theory]
    [InlineData(19, 0, 19, 0, true)]
    [InlineData(18, 0, 18, 0, false)]
    [InlineData(17, 0, 19, 0, false)]
    [InlineData(19, 0, 17, 0, false)]
    [InlineData(19, 1, 19, 1, false)]
    [InlineData(19, 0, 19, 1, false)]
    public void should_admit_only_the_released_exact_pairs(int languageMajor, int languageMinor, int semanticMajor, int semanticMinor, bool supported)
    {
        var model = SemanticModelSerializer.Deserialize(InvocationRefusalsCorpus.V19.EsmBytes.AsSpan());
        typeof(ExecutableSemanticModel).GetField("<LanguageVersion>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(model, new LanguageVersion((uint)languageMajor, (uint)languageMinor));
        typeof(ExecutableSemanticModel).GetField("<SemanticVersion>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(model, new SemanticVersion((uint)semanticMajor, (uint)semanticMinor));
        SemanticVersionFeatures.Supports(model).ShouldEqual(supported);
    }

    [Fact]
    public void should_preserve_all_older_canonical_bytes()
    {
        foreach (var version in Enumerable.Range(1, 17))
        {
            var vector = EsmGoldenVectors.Get(version);
            var model = SemanticModelSerializer.Deserialize(vector.Bytes.AsSpan());
            SemanticVersionFeatures.Supports(model).ShouldBeTrue();
            Assert.Equal(vector.Bytes.ToArray(), SemanticModelSerializer.Serialize(model));
        }
    }
}
