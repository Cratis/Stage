// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticChecksumRules;

public class when_reading_screenplay_checksum_vectors
{
    // Validation vectors copied from Cratis/Screenplay v4.180.1:
    // Source/Screenplay/Compiler/Conformance/generation-checksums.json, validation array.
    // Consumed there by Source/DotNET/Screenplay/Semantics/Generation/for_GenerationChecksum/when_reading_shared_checksum_vectors.cs.
    [Theory]
    [InlineData("0", "luhn", true)]
    [InlineData("0", "mod11", true)]
    [InlineData("98", "mod97", true)]
    [InlineData("79927398713", "luhn", true)]
    [InlineData("A1-B2-C30", "luhn", true)]
    [InlineData("A1-B2-C36", "mod11", true)]
    [InlineData("A1-B2-C320", "mod97", true)]
    [InlineData("", "luhn", false)]
    [InlineData("", "mod11", false)]
    [InlineData("8", "mod97", false)]
    [InlineData("00", "mod97", false)]
    [InlineData("79927398714", "luhn", false)]
    [InlineData("79927398713\n", "luhn", false)]
    [InlineData("000000006-A0", "mod11", false)]
    [InlineData("000000006-A10", "mod11", false)]
    [InlineData("A1-B2-C320 ", "mod97", false)]
    [InlineData("A1-B2-C3２０", "mod97", false)]
    [InlineData("９0", "luhn", false)]
    [InlineData("A1-B2-C3-20", "mod97", true)]
    [InlineData("195", "mod97", true)]
    [InlineData("198", "mod97", false)]
    [InlineData("6209", "mod97", true)]
    [InlineData("62106", "mod97", false)]
    [InlineData("01", "mod97", false)]
    public void should_match_the_frozen_validation_result(string value, string algorithm, bool valid) =>
        SemanticChecksumRules.Satisfies(SemanticValue.Text(value), SemanticValue.Text(algorithm), () => new InvalidSemanticContract("A checksum vector has an unsupported shape.")).ShouldEqual(valid);
}
