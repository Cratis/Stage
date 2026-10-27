// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Emission;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_LocalFileSystemOutput;

public class when_writing_a_nested_path_inside_the_root : Specification
{
    const string Content = "Generated content";
    DirectoryInfo _target = null!;
    StringWriter _output = null!;

    void Establish()
    {
        _target = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), $"stage-output-{Guid.NewGuid():N}"));
        _output = new StringWriter();
    }

    async Task Because() => await new LocalFileSystemOutput().Write(new RenderedFile("a/b/c.cs", Content), _target, _output);

    void Destroy()
    {
        _output.Dispose();
        _target.Delete(recursive: true);
    }

    [Fact] void should_write_the_content() => File.ReadAllText(Path.Combine(_target.FullName, "a", "b", "c.cs")).ShouldEqual(Content);
    [Fact] void should_report_the_relative_path() => _output.ToString().ShouldEqual($"Writing a/b/c.cs{Environment.NewLine}");
}
