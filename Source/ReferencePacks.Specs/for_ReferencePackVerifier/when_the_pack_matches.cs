// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Cratis.ReferencePacks.for_ReferencePackVerifier;

public class when_the_pack_matches : given.a_reference_pack
{
    void Because() => _errors = [.. ReferencePackVerifier.Verify(_packDirectory, _manifestPath)];

    [Fact] void should_accept_the_pack() => _errors.ShouldBeEmpty();
}
