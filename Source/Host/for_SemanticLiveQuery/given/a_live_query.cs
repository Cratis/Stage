// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Semantics;

namespace Cratis.Stage.Host.for_SemanticLiveQuery.given;

public class a_live_query : Specification
{
    internal a_fake_world _world = null!;
    internal SemanticLiveQuery<string> _query = null!;
    protected Func<Task<string>> _read = null!;

    void Establish()
    {
        _world = new();
        _read = () => Task.FromResult("initial");
        _query = new(_world, () => _read(), "initial");
    }
}
