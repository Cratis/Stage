// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Cratis.ReferencePacks.for_ReferencePackVerifier;

public class when_the_manifest_is_empty : given.a_reference_pack
{
    Exception _error;

    void Establish() => File.WriteAllText(_manifestPath, string.Empty);

    void Because() => _error = Catch.Exception(() => _errors = [.. ReferencePackVerifier.Verify(_packDirectory, _manifestPath)]);

    [Fact] void should_fail_closed() => _error.ShouldBeOfExactType<InvalidDataException>();
}
