// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Scene.for_CompositionConverter.when_converting_with_diagnostics;

public class and_operations_are_not_a_collection : Specification
{
    readonly ConversionDiagnostics _diagnostics = new();

    void Because() => CompositionConverter.Exposure(new ExposureSyntax("Shell", [new ExposedPropertySyntax("title", "text", SourceLocation.Start) { Operations = ["unknown"] }], SourceLocation.Start), _diagnostics);

    [Fact] void should_not_validate_operations_ignored_by_the_legacy_converter() => _diagnostics.All.ShouldBeEmpty();
}
