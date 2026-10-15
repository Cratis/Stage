// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticVersionFeatures;

public class when_admitting_v17 : Specification
{
    [Theory]
    [InlineData(17, 0, 17, 0, true)]
    [InlineData(17, 0, 16, 0, false)]
    [InlineData(16, 0, 17, 0, false)]
    [InlineData(17, 1, 17, 1, false)]
    [InlineData(18, 0, 18, 0, true)]
    [InlineData(20, 0, 20, 0, true)]
    public void should_admit_only_the_exact_audited_pair(int languageMajor, int languageMinor, int semanticMajor, int semanticMinor, bool supported)
    {
        var model = SemanticModelSerializer.Deserialize(EsmGoldenVectors.Get(17).Bytes.AsSpan());
        // Simulate a future reader without broadening Stage's exact-pair allowlist.
        typeof(ExecutableSemanticModel).GetField("<LanguageVersion>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(model, new LanguageVersion((uint)languageMajor, (uint)languageMinor));
        typeof(ExecutableSemanticModel).GetField("<SemanticVersion>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(model, new SemanticVersion((uint)semanticMajor, (uint)semanticMinor));
        SemanticVersionFeatures.Supports(model).ShouldEqual(supported);
    }

    [Fact]
    public void should_preserve_every_older_vector_byte_for_byte()
    {
        foreach (var version in Enumerable.Range(1, 16))
        {
            var vector = EsmGoldenVectors.Get(version);
            var model = SemanticModelSerializer.Deserialize(vector.Bytes.AsSpan());
            SemanticVersionFeatures.Supports(model).ShouldBeTrue();
            Assert.Equal(vector.Bytes.ToArray(), SemanticModelSerializer.Serialize(model));
        }
    }
}
