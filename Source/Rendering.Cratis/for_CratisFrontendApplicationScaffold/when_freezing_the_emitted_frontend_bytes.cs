// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisFrontendApplicationScaffold.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisFrontendApplicationScaffold;

public class when_freezing_the_emitted_frontend_bytes : a_current_frontend_scaffold
{
    string _digest = null!;

    void Because()
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        foreach (var input in _first)
        {
            hash.AppendData(Encoding.UTF8.GetBytes(input.Name));
            hash.AppendData(input.Bytes.AsSpan());
        }

        _digest = Convert.ToHexStringLower(hash.GetHashAndReset());
    }

    [Fact] void should_freeze_the_complete_frontend_scaffold_with_the_arc_22505_and_scene_410_contract() => _digest.ShouldEqual("ca1cf6e03d74f59566c21c99adf18275ee8f24d3aa045d60377d40e401ad0c23");
}
