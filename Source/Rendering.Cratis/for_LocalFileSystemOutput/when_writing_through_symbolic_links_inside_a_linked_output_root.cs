// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Emission;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_LocalFileSystemOutput;

public class when_writing_through_symbolic_links_inside_a_linked_output_root : Specification
{
    DirectoryInfo _temporary = null!;
    DirectoryInfo _target = null!;
    string _destination = null!;
    bool _symlinksSupported = true;
    Exception? _error;

    void Establish()
    {
        _temporary = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), $"stage-output-{Guid.NewGuid():N}"));
        var actual = _temporary.CreateSubdirectory("actual");
        var root = _temporary.CreateSubdirectory("physical-root");
        var nested = root.CreateSubdirectory("nested");
        _destination = Path.Combine(root.FullName, "destination.cs");
        File.WriteAllText(_destination, "original");
        _target = new DirectoryInfo(Path.Combine(_temporary.FullName, "alias", "out"));
        try
        {
            Directory.CreateSymbolicLink(Path.Combine(actual.FullName, "out"), root.FullName);
            Directory.CreateSymbolicLink(Path.Combine(_temporary.FullName, "alias"), actual.FullName);
            Directory.CreateSymbolicLink(Path.Combine(root.FullName, "link"), nested.FullName);
            File.CreateSymbolicLink(Path.Combine(nested.FullName, "file.cs"), _destination);
        }
        catch (Exception error) when (OperatingSystem.IsWindows() && (error is UnauthorizedAccessException or IOException))
        {
            // Unprivileged Windows may not allow symbolic links; Linux CI always exercises the write.
            _symlinksSupported = false;
        }
    }

    async Task Because()
    {
        if (_symlinksSupported)
        {
            _error = await Catch.Exception(() => new LocalFileSystemOutput().Write(new RenderedFile("link/file.cs", "replacement"), _target, TextWriter.Null));
        }
    }

    void Destroy() => _temporary.Delete(recursive: true);

    [Fact] void should_allow_the_path() => _error.ShouldBeNull();
    [Fact] void should_write_the_resolved_file() => (!_symlinksSupported || File.ReadAllText(_destination) == "replacement").ShouldBeTrue();
}
