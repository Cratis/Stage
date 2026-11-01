// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Renderers.for_LegacyGeneratedTypes;

public class when_rendering_context_support : an_application_reading_the_context
{
    async Task Because() => await _renderer.Render([_application], _targetDirectory, _output, _error);

    [Fact] void should_emit_both_support_files() =>
        _codeOutput.Files.Count(file => file.RelativePath.StartsWith("Generated", StringComparison.Ordinal)).ShouldEqual(2);
    [Fact] void should_inventory_every_emitted_declaration() =>
        InventoryAssertions.Missing(_codeOutput.Files, new([_application]), "AcmeBilling").ShouldBeEmpty();
}
