// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Cratis.ReferencePacks.for_ReferencePackVerifier;

public class when_an_assembly_is_missing : given.a_reference_pack
{
    void Establish() => File.Delete(_assemblyPath);

    void Because() => _errors = [.. ReferencePackVerifier.Verify(_packDirectory, _manifestPath)];

    [Fact] void should_name_the_missing_file() => _errors.ShouldContainOnly($"Missing reference assembly '{_assemblyPath}'.");
}
