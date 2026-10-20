// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisBackendApplicationScaffold.given;
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
            hash.AppendData(Encoding.UTF8.GetBytes(FrozenScaffoldPins.Normalize(PathOf(input), Text(input))));
        }

        _digest = Convert.ToHexStringLower(hash.GetHashAndReset());
    }

    [Fact] void should_freeze_the_complete_frontend_shell_independently_of_package_releases() => _digest.ShouldEqual("7c3799d2d7b1f40defb57c614908bd0875df860edf03d36eb6dc930044ab7418");

    [Fact] void should_preserve_the_previous_entry_point_bytes_except_for_the_optional_stage_import() =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(PreviousFrontendEntryPoint.Restore(".frontend/main.tsx", Content(".frontend/main.tsx"))))).ShouldEqual("f1be33ab4c4b17540e101e671276f76ac33b0b9f79f1cd536f2f1cf5eb235d65");
}
