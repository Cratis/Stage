// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRenderer;

public class when_rendering_a_single_slice : an_application_with_cross_slice_references
{
    ApplicationSet _context = null!;

    void Establish() => _context = new ApplicationSet([_application]);

    async Task Because() =>
        await _renderer.Render(_summarySlice, _context, _targetDirectory, _output, _error, module: "Billing", feature: "Reporting");

    [Fact] void should_write_only_the_one_slice() => _codeOutput.Files.Count.ShouldEqual(1);
    [Fact] void should_place_it_under_the_given_module_and_feature() =>
        _codeOutput.Files[0].RelativePath.ShouldEqual(Path.Combine("Billing", "Reporting", "Summary", "Summary.cs"));
    [Fact] void should_resolve_types_against_the_surrounding_application() =>
        _codeOutput.Files[0].Content.ShouldContain("using AcmeBilling.Billing.Registration.Register;");
    [Fact] void should_namespace_it_by_the_given_module_and_feature() =>
        _codeOutput.Files[0].Content.ShouldContain("namespace AcmeBilling.Billing.Reporting.Summary;");
}
