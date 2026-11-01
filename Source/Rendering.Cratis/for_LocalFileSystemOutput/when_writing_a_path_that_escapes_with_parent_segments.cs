// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Emission;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_LocalFileSystemOutput;

public class when_writing_a_path_that_escapes_with_parent_segments : Specification
{
    DirectoryInfo _temporary = null!;
    DirectoryInfo _target = null!;
    Exception? _error;

    void Establish()
    {
        _temporary = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), $"stage-output-{Guid.NewGuid():N}"));
        _target = new DirectoryInfo(Path.Combine(_temporary.FullName, "out"));
    }

    async Task Because() => _error = await Catch.Exception(() => new LocalFileSystemOutput().Write(new RenderedFile("../escaped.cs", "content"), _target, TextWriter.Null));

    void Destroy() => _temporary.Delete(recursive: true);

    [Fact] void should_refuse_the_path() => _error.ShouldBeOfExactType<GeneratedPathOutsideOutputRoot>();
    [Fact] void should_not_write_outside_the_root() => File.Exists(Path.Combine(_temporary.FullName, "escaped.cs")).ShouldBeFalse();
    [Fact] void should_not_create_the_output_root() => Directory.Exists(_target.FullName).ShouldBeFalse();
    [Fact] void should_name_the_relative_path() => _error!.Message.ShouldContain("../escaped.cs");
    [Fact] void should_name_the_output_root() => _error!.Message.ShouldContain(_target.FullName);
}
