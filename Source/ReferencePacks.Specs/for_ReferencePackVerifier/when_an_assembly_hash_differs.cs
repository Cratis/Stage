// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Cratis.ReferencePacks.for_ReferencePackVerifier;

public class when_an_assembly_hash_differs : given.a_reference_pack
{
    void Establish() => File.WriteAllText(_assemblyPath, "altered assembly");

    void Because() => _errors = [.. ReferencePackVerifier.Verify(_packDirectory, _manifestPath)];

    [Fact] void should_reject_the_assembly() => _errors.Length.ShouldEqual(1);
    [Fact] void should_name_the_mismatching_file() => _errors[0].ShouldContain($"SHA-256 mismatch for reference assembly '{_assemblyPath}'");
}
