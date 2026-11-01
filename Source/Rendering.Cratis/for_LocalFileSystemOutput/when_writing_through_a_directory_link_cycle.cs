// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Emission;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_LocalFileSystemOutput;

public class when_writing_through_a_directory_link_cycle : Specification
{
    DirectoryInfo _target = null!;
    Exception? _error;

    void Establish()
    {
        _target = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), $"stage-output-{Guid.NewGuid():N}"));
        Directory.CreateSymbolicLink(Path.Combine(_target.FullName, "a"), "a/child");
    }

    async Task Because() => _error = await Catch.Exception(() => new LocalFileSystemOutput().Write(new RenderedFile("a/file.cs", "content"), _target, TextWriter.Null));

    void Destroy() => _target.Delete(recursive: true);

    [Fact] void should_refuse_the_cycle() => _error.ShouldBeOfExactType<GeneratedPathOutsideOutputRoot>();
}
