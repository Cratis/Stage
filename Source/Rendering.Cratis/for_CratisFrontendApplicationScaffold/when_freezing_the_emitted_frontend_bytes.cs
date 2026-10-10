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

    [Fact] void should_freeze_the_complete_frontend_shell_with_the_arc_22582_and_scene_416_contract() => _digest.ShouldEqual("b27331faa5cb31cb4786077e428de2ae0b6470b5d6adf8f2958b7c383b1cac01");

    static bool IsRuntime(string path) =>
        path.StartsWith(".frontend/stage/", StringComparison.Ordinal) || path == ".frontend/styledPrimeReact.ts";
}
