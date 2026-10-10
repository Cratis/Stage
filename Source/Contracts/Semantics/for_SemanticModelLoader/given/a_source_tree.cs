// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;

namespace Cratis.Stage.Contracts.Semantics.for_SemanticModelLoader.given;

public class a_source_tree : Specification
{
    protected string _workspace = null!;
    protected string _root = null!;
    protected string _outside = null!;
    protected SemanticModelLoadResult _result = null!;

    void Establish()
    {
        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (!File.Exists(Path.Combine(directory.FullName, "Stage.slnx"))) directory = directory.Parent!;
        _workspace = Path.Combine(directory.FullName, ".ai-work", "210", "link-specs", Guid.NewGuid().ToString("N"));
        _root = Path.Combine(_workspace, "root");
        _outside = Path.Combine(_workspace, "outside");
        Directory.CreateDirectory(Path.Combine(_root, "nested"));
        Directory.CreateDirectory(Path.Combine(_outside, "nested"));
        const string source = """
            module Projects
              feature Registration
                slice StateChange RegisterProject
                  command RegisterProject
                    projectId Uuid identifier
                    produces ProjectRegistered
                      for projectId
                  event ProjectRegistered
            """;
        File.WriteAllText(Path.Combine(_root, "nested", "source.play"), source);
        File.WriteAllText(Path.Combine(_outside, "nested", "source.play"), "not valid screenplay !!!");
    }

    void Destroy() => Directory.Delete(_workspace, recursive: true);
}
#endif
