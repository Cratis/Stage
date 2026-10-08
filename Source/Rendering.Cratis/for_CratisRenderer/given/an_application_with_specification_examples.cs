// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRenderer.given;

public class an_application_with_specification_examples : a_multi_slice_application
{
    void Establish()
    {
        var compilation = new ScreenplayCompiler().Compile("""
            module Projects
              feature Registration
                slice StateChange Register
                  command RegisterProject
                    projectId String identifier
                    name String
                    produces ProjectRegistered
                      name = name
                  event ProjectRegistered
                    name String
                  example Input : RegisterProject
                    projectId = "project-1"
                    name = "original"
                  example Fact : ProjectRegistered
                    name = "original"
                  specification RegisteringAProject
                    when Input name = "changed"
                    then Fact
                      name = "changed"
            """);
        Assert.True(compilation.Success, string.Join("; ", compilation.Diagnostics));
        _application = compilation.Value!;
    }
}
