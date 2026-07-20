// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.for_EventModelLoader;

public class when_loading_invalid_input : Specification
{
    Exception? _compileFailure;
    Exception? _missingDirectory;
    Exception? _emptyDirectory;

    async Task Because()
    {
        _compileFailure = Catch.Exception(() => EventModelLoader.LoadFromSource("module"));
        _missingDirectory = await Catch.Exception(() => EventModelLoader.LoadFromDirectoryAsync(Path.Combine(Path.GetTempPath(), $"stage-missing-{Guid.NewGuid():N}")));

        var empty = Path.Combine(Path.GetTempPath(), $"stage-empty-{Guid.NewGuid():N}");
        Directory.CreateDirectory(empty);
        _emptyDirectory = await Catch.Exception(() => EventModelLoader.LoadFromDirectoryAsync(empty));
        Directory.Delete(empty, recursive: true);
    }

    [Fact] void should_reject_source_that_fails_to_compile() => (_compileFailure is InvalidEventModel).ShouldBeTrue();
    [Fact] void should_surface_the_diagnostics() => _compileFailure!.Message.Contains("Invalid module declaration", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_reject_a_missing_directory() => (_missingDirectory is InvalidEventModel).ShouldBeTrue();
    [Fact] void should_reject_a_directory_without_play_files() => (_emptyDirectory is InvalidEventModel).ShouldBeTrue();
}
