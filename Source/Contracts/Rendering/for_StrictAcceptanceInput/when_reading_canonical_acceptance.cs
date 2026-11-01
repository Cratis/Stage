// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Rendering.for_StrictAcceptanceInput;

public class when_reading_canonical_acceptance
{
    [Theory]
    [InlineData(false, "false")]
    [InlineData(true, "true")]
    public void should_roundtrip_exact_utf8_without_a_newline(bool strict, string text)
    {
        var input = StrictAcceptanceInput.Create(strict);
        input.Name.ShouldEqual("stage:strict");
        input.Version.ShouldEqual("1");
        Encoding.UTF8.GetString(input.Bytes.AsSpan()).ShouldEqual(text);
        StrictAcceptanceInput.TryRead(input, out var actual).ShouldBeTrue();
        actual.ShouldEqual(strict);
    }

    [Theory]
    [InlineData("TRUE", "1")]
    [InlineData("true\n", "1")]
    [InlineData(" true", "1")]
    [InlineData("1", "1")]
    [InlineData("true", "2")]
    public void should_reject_noncanonical_inputs(string text, string version)
    {
        var input = ArtifactRenderInput.Create("stage:strict", version, [.. Encoding.UTF8.GetBytes(text)]);
        StrictAcceptanceInput.TryRead(input, out var strict).ShouldBeFalse();
        strict.ShouldBeFalse();
    }
}
