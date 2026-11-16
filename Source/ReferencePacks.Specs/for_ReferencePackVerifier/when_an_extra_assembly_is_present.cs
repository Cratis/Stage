// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Cratis.ReferencePacks.for_ReferencePackVerifier;

public class when_an_extra_assembly_is_present : given.a_reference_pack
{
    string _extraAssembly;

    void Establish()
    {
        _extraAssembly = Path.Combine(_packDirectory, "Extra.dll");
        File.WriteAllBytes(_extraAssembly, []);
    }

    void Because() => _errors = [.. ReferencePackVerifier.Verify(_packDirectory, _manifestPath)];

    [Fact] void should_name_the_extra_file() => _errors.ShouldContainOnly($"Unexpected reference assembly '{_extraAssembly}'.");
}
