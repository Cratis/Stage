// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_EsmSupport.when_classifying.given;

public class a_descriptor : Specification
{
    private protected EsmSupport _support = null!;
    private protected EsmVersionStanding _standing;

    void Establish() => _support = new([(LanguageVersion.V30, SemanticVersion.V30)], new(SemanticVersion.V32, 33));
}
