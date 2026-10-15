// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Cratis.Stage.Contracts.Semantics.for_SemanticModelLoader.given;

public class a_source_with_notes : a_source_tree
{
    protected string _path = null!;

    void Establish()
    {
        _path = Path.Combine(_root, "nested", "source.play");
        File.WriteAllText(_path, """
            module Projects
              feature Registration
                slice StateChange RegisterProject
                  command RegisterProject
                    description "Register a project"
                    projectId Uuid identifier
                    produces ProjectRegistered
                      for projectId
                  event ProjectRegistered
                    description "A project was registered"
            """);
    }
}
#endif
