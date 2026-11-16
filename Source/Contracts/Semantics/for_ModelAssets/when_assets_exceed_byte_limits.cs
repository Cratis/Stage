// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Diagnostics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Semantics.for_ModelAssets;

public class when_assets_exceed_byte_limits : given.an_asset_directory
{
    void Establish()
    {
        for (var index = 0; index < 5; index++) Write($"{index}.bin", new byte[5 * 1024 * 1024]);
        Write("5.bin", [1]);
        Write("oversized.bin", new byte[(5 * 1024 * 1024) + 1]);
    }

    void Because() => _result = ModelAssets.Read(_root);
    [Fact] void should_admit_exactly_the_total_limit() => _result.Files.Values.Sum(bytes => bytes.Length).ShouldEqual(25 * 1024 * 1024);
    [Fact] void should_omit_excess_files() => _result.Files.Count.ShouldEqual(5);
    [Fact] void should_warn_without_errors() => _result.Diagnostics.Select(diagnostic => diagnostic.Severity).ShouldEqual([DiagnosticSeverity.Warning, DiagnosticSeverity.Warning]);
}
#endif
