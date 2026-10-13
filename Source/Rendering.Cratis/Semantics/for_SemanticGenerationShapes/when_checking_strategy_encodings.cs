// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticGenerationShapes;

public class when_checking_strategy_encodings : Specification
{
    [Theory]
    [InlineData("ImplicitId", "00112233-4455-4677-8899-aabbccddeeff", true)]
    [InlineData("ImplicitId", "11111111-1111-1111-1111-111111111111", false)]
    [InlineData("ImplicitId", "00112233-4455-4677-8899-AABBCCDDEEFF", false)]
    [InlineData("Id", "00112233-4455-7677-8899-aabbccddeeff", true)]
    [InlineData("Id", "00112233-4455-4677-8899-aabbccddeeff", false)]
    [InlineData("StringUuid", "ref_00112233-4455-4677-8899-AABBCCDDEEFF", true)]
    [InlineData("StringUuid", "REF_00112233-4455-4677-8899-AABBCCDDEEFF", false)]
    [InlineData("SortableId", "01arz3ndektsv4rrffq69g5fav", true)]
    [InlineData("SortableId", "81arz3ndektsv4rrffq69g5fav", false)]
    [InlineData("UserId", "user_0000000000e008000000000000", true)]
    [InlineData("UserId", "user_0000000000d008000000000000", false)]
    [InlineData("UserId", "user_0000000000e004000000000000", false)]
    [InlineData("BareTypeId", "0000000000e008000000000000", true)]
    [InlineData("BareTypeId", "_0000000000e008000000000000", false)]
    [InlineData("Receipt", "ABCDEFGHIJKLMNOPQRSTU", true)]
    [InlineData("Receipt", "ABCDEFGHIJKLMNOPQRST!", false)]
    [InlineData("HexReference", "0123456789ABCDEF0123456789ABCDEF", true)]
    [InlineData("HexReference", "0123456789abcdef0123456789abcdef", false)]
    [InlineData("Base32Reference", "aaaaaaaaaaaaaaaaaaaaaaaaaa", true)]
    [InlineData("Base32Reference", "aaaaaaaaaaaaaaaaaaaaaaaaab", false)]
    [InlineData("EncodedRandom", "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA", true)]
    [InlineData("EncodedRandom", "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAB", false)]
    public void should_preserve_the_exact_strategy_shape(string name, string value, bool valid) =>
        SemanticGenerationShapes.Matches(a_generation_model.Model().Application.Concepts.Single(concept => concept.Name == name), SemanticValue.Text(value)).ShouldEqual(valid);
}
