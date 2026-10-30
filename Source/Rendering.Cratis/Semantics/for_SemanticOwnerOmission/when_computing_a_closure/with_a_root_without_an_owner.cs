// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticOwnerOmission.when_computing_a_closure;

public class with_a_root_without_an_owner : given.an_omission_context
{
    SemanticOwnerOmission? _omission;

    void Because()
    {
        BuildContext();
        _omission = SemanticOwnerOmission.Compute(_context, _index, Roots(Identity("absent")));
    }

    [Fact] void should_not_be_provable() => _omission.ShouldBeNull();
}
