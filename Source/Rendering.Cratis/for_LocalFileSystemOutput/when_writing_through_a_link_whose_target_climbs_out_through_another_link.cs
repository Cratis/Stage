// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Emission;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_LocalFileSystemOutput;

public class when_writing_through_a_link_whose_target_climbs_out_through_another_link : Specification
{
    const string ExistingContent = "User-owned content";
    DirectoryInfo _temporary = null!;
    DirectoryInfo _target = null!;
    string _victim = null!;
    Exception? _error;

    void Establish()
    {
        _temporary = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), $"stage-output-{Guid.NewGuid():N}"));
        _target = _temporary.CreateSubdirectory("out");
        var outside = _temporary.CreateSubdirectory("outside");
        var nested = outside.CreateSubdirectory("nested");
        _victim = Path.Combine(outside.FullName, "victim.cs");
        File.WriteAllText(_victim, ExistingContent);
        Directory.CreateSymbolicLink(Path.Combine(_target.FullName, "hop"), nested.FullName);
        File.CreateSymbolicLink(Path.Combine(_target.FullName, "link.cs"), "hop/../victim.cs");
    }

    async Task Because() => _error = await Catch.Exception(() => new LocalFileSystemOutput().Write(new RenderedFile("link.cs", "replacement"), _target, TextWriter.Null));

    void Destroy() => _temporary.Delete(recursive: true);

    [Fact] void should_refuse_the_path() => _error.ShouldBeOfExactType<GeneratedPathOutsideOutputRoot>();
    [Fact] void should_not_overwrite_the_outside_file() => File.ReadAllText(_victim).ShouldEqual(ExistingContent);
}
