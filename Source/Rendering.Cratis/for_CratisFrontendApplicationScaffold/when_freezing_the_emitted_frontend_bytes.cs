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

        // The shell only: the Stage runtime beside it is the live Stage frontend's own source, compared with that
        // source byte for byte by when_emitting_the_stage_frontend_runtime, so freezing it here as well would
        // only make every frontend change update this digest.
        foreach (var input in _first.Where(input => !IsRuntime(PathOf(input))))
        {
            hash.AppendData(Encoding.UTF8.GetBytes(input.Name));
            hash.AppendData(input.Bytes.AsSpan());
        }

        _digest = Convert.ToHexStringLower(hash.GetHashAndReset());
    }

    [Fact] void should_freeze_the_complete_frontend_shell_with_the_arc_22582_and_scene_4620_contract() => _digest.ShouldEqual("bef2a298860a7e8c0bd1fb09f3c140ed3e1b4c822d3099f02e58b56407a93bb9");

    [Fact] void should_preserve_the_previous_entry_point_bytes_except_for_the_optional_stage_import() =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(PreviousFrontendEntryPoint.Restore(".frontend/main.tsx", Content(".frontend/main.tsx"))))).ShouldEqual("f1be33ab4c4b17540e101e671276f76ac33b0b9f79f1cd536f2f1cf5eb235d65");

    static bool IsRuntime(string path) =>
        path.StartsWith(".frontend/stage/", StringComparison.Ordinal) || path == ".frontend/styledPrimeReact.ts";
}
