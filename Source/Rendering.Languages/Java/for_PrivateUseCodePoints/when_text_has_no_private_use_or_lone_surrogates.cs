// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_PrivateUseCodePoints;

public class when_text_has_no_private_use_or_lone_surrogates : Specification
{
    Exception?[] _errors = [];

    void Because() => _errors = [.. new[] { 0xD7FF, 0xF900, 0xEFFFF, 0xFFFFE, 0xFFFFF, 0x10FFFE, 0x10FFFF, 0x1F600 }
        .Select(char.ConvertFromUtf32).Append(@"\ud83d\ude00").Append(string.Empty)
        .Select(text => Catch.Exception(() => PrivateUseCodePoints.Validate(text)))];

    [Fact] void should_accept_non_private_use_boundaries_and_valid_surrogate_pairs() => _errors.ShouldEachConformTo(error => error == null);
}
