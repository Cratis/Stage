// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Diagnostics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Semantics.for_SemanticModelLoadResult;

public class when_classifying_diagnostic_severity : Specification
{
    readonly SemanticModelLoadDiagnostic _information = new("INFO", "Information", "model.play(1,1)") { Severity = DiagnosticSeverity.Information };
    readonly SemanticModelLoadDiagnostic _error = new("ERROR", "Error", "model.play(2,1)");
    readonly SemanticModelLoadDiagnostic _warning = new("WARN", "Warning", "model.play(3,1)") { Severity = DiagnosticSeverity.Warning };
    SemanticModelLoadResult _result = null!;
    SemanticModelLoadResult _revised = null!;

    void Establish() => _result = new(null, [_information, _error, _warning]);

    void Because() => _revised = _result with { Diagnostics = [_warning, _information] };

    [Fact] void should_preserve_the_complete_ordered_diagnostic_stream() => _result.Diagnostics.SequenceEqual([_information, _error, _warning]).ShouldBeTrue();
    [Fact] void should_filter_notes_without_changing_their_order() => _result.Notes.SequenceEqual([_information, _warning]).ShouldBeTrue();
    [Fact] void should_filter_errors() => _result.Errors.ShouldContainOnly(_error);
    [Fact] void should_derive_notes_from_the_current_diagnostics() => _revised.Notes.SequenceEqual([_warning, _information]).ShouldBeTrue();
    [Fact] void should_derive_errors_from_the_current_diagnostics() => _revised.Errors.ShouldBeEmpty();
}
#endif
