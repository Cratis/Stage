// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Kits.for_LanguagePackVersion;

public class when_parsing : Specification
{
    [Theory]
    [InlineData("java@1.0.0", true)]
    [InlineData("csharp@1.2.3-preview.1", true)]
    [InlineData("java@^1.0.0", false)]
    [InlineData("java@1.x", false)]
    [InlineData("java@1", false)]
    [InlineData("java@>=1.0.0", false)]
    [InlineData("java@*", false)]
    [InlineData("java@1.0.0-01", false)]
    void should_accept_only_exact_versions(string text, bool accepted) => LanguagePackVersion.TryParse(text, out _).ShouldEqual(accepted);
}
