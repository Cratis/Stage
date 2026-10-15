// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Diagnostics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Semantics.for_SemanticModelLoader;

public class when_loading_warning_diagnostics : given.a_source_with_notes
{
    void Establish() => File.AppendAllText(_path, "\nconcept Status : Enum\n  draft\n  validate\n  sent\n");

    async Task Because() => _result = await SemanticModelLoader.LoadAsync(_root, [], null, "Projects");

    [Fact] void should_load_successfully() => _result.Success.ShouldBeTrue();
    [Fact] void should_not_report_blocking_diagnostics() => _result.Errors.ShouldBeEmpty();
    [Fact] void should_keep_all_diagnostics_for_existing_consumers() => _result.Diagnostics.Length.ShouldEqual(3);
    [Fact] void should_preserve_the_warning_as_a_note() => _result.Notes.Count(note => note.Severity == DiagnosticSeverity.Warning).ShouldEqual(1);
    [Fact] void should_also_preserve_information() => _result.Notes.Count(note => note.Severity == DiagnosticSeverity.Information).ShouldEqual(2);
}
#endif
