// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Emission;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_LocalFileSystemOutput;

public class when_writing_into_a_sibling_with_a_shared_prefix : Specification
{
    DirectoryInfo _temporary = null!;
    DirectoryInfo _target = null!;
    string _outside = null!;
    Exception? _error;

    void Establish()
    {
        _temporary = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), $"stage-output-{Guid.NewGuid():N}"));
        _target = _temporary.CreateSubdirectory("out");
        _outside = Path.Combine(_temporary.FullName, "out-evil");
    }

    async Task Because() => _error = await Catch.Exception(() => new LocalFileSystemOutput().Write(new RenderedFile("../out-evil/x.cs", "content"), _target, TextWriter.Null));

    void Destroy() => _temporary.Delete(recursive: true);

    [Fact] void should_refuse_the_path() => _error.ShouldBeOfExactType<GeneratedPathOutsideOutputRoot>();
    [Fact] void should_not_write_outside_the_root() => File.Exists(Path.Combine(_outside, "x.cs")).ShouldBeFalse();
    [Fact] void should_not_create_the_sibling_directory() => Directory.Exists(_outside).ShouldBeFalse();
}
