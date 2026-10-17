// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticVersionFeatures;

public class when_admitting_v22 : Specification
{
    [Theory]
    [InlineData(22, 0, 22, 0, true)]
    [InlineData(21, 0, 22, 0, false)]
    [InlineData(22, 0, 21, 0, false)]
    [InlineData(22, 1, 22, 1, false)]
    public void should_admit_only_the_audited_exact_pair(int languageMajor, int languageMinor, int semanticMajor, int semanticMinor, bool supported)
    {
        var model = SemanticModelSerializer.Deserialize(EsmGoldenVectors.Get(22).Bytes.AsSpan());
        typeof(ExecutableSemanticModel).GetField("<LanguageVersion>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(model, new LanguageVersion((uint)languageMajor, (uint)languageMinor));
        typeof(ExecutableSemanticModel).GetField("<SemanticVersion>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(model, new SemanticVersion((uint)semanticMajor, (uint)semanticMinor));
        SemanticVersionFeatures.Supports(model).ShouldEqual(supported);
    }
}
