// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRenderer;

public class when_rendering_a_single_feature : an_application_with_cross_slice_references
{
    ApplicationSet _context = null!;

    void Establish() => _context = new ApplicationSet([_application]);

    async Task Because() => await _renderer.Render(_reportingFeature, _context, _targetDirectory, _output, _error, module: "Billing");

    [Fact] void should_write_only_the_slices_of_that_feature() => _codeOutput.Files.Count.ShouldEqual(1);
    [Fact] void should_place_them_under_the_given_module() =>
        _codeOutput.Files[0].RelativePath.ShouldEqual(Path.Combine("Billing", "Reporting", "Summary", "Summary.cs"));
}
