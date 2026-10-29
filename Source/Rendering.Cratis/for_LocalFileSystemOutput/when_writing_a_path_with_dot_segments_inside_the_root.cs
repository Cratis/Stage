// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Emission;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_LocalFileSystemOutput;

public class when_writing_a_path_with_dot_segments_inside_the_root : Specification
{
    DirectoryInfo _target = null!;
    StringWriter _output = null!;

    void Establish()
    {
        _target = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), $"stage-output-{Guid.NewGuid():N}"));
        _output = new StringWriter();
    }

    async Task Because() => await new LocalFileSystemOutput().Write(new RenderedFile("a/../b.cs", "content"), _target, _output);

    void Destroy()
    {
        _output.Dispose();
        _target.Delete(recursive: true);
    }

    [Fact] void should_write_the_normalized_path() => File.ReadAllText(Path.Combine(_target.FullName, "b.cs")).ShouldEqual("content");
    [Fact] void should_report_the_original_relative_path() => _output.ToString().ShouldEqual($"Writing a/../b.cs{Environment.NewLine}");
}
