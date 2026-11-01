// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Renderers.for_LegacyGeneratedTypes;

public class when_rendering_a_multi_slice_application : a_multi_slice_application
{
    async Task Because() => await _renderer.Render([_application], _targetDirectory, _output, _error);

    [Fact] void should_render_declarations_to_check() => _codeOutput.Files.Count.ShouldBeGreaterThan(0);
    [Fact] void should_inventory_every_emitted_declaration() =>
        InventoryAssertions.Missing(_codeOutput.Files, new([_application]), "CratisRendererSpec").ShouldBeEmpty();
}
