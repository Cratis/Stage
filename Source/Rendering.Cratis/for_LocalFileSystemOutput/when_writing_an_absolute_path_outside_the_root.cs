// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Emission;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_LocalFileSystemOutput;

public class when_writing_an_absolute_path_outside_the_root : Specification
{
    DirectoryInfo _temporary = null!;
    DirectoryInfo _target = null!;
    string _outside = null!;
    string _path = null!;
    Exception? _error;

    void Establish()
    {
        _temporary = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), $"stage-output-{Guid.NewGuid():N}"));
        _target = _temporary.CreateSubdirectory("out");
        _outside = Path.Combine(_temporary.FullName, "outside");
        _path = Path.Combine(_outside, "escaped.cs");
    }

    async Task Because() => _error = await Catch.Exception(() => new LocalFileSystemOutput().Write(new RenderedFile(_path, "content"), _target, TextWriter.Null));

    void Destroy() => _temporary.Delete(recursive: true);

    [Fact] void should_refuse_the_path() => _error.ShouldBeOfExactType<GeneratedPathOutsideOutputRoot>();
    [Fact] void should_not_write_outside_the_root() => File.Exists(_path).ShouldBeFalse();
    [Fact] void should_not_create_the_outside_directory() => Directory.Exists(_outside).ShouldBeFalse();
}
