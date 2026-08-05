// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Rendering.Cratis.Scaffolding;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRenderer.given;

public class a_stub_scaffolder : IProjectScaffolder
{
    public bool WasCalled { get; private set; }

    public Task<bool> EnsureScaffolded(DirectoryInfo targetDirectory, TextWriter output)
    {
        WasCalled = true;
        return Task.FromResult(false);
    }
}
