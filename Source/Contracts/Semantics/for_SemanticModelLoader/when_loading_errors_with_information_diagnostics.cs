// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Diagnostics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Semantics.for_SemanticModelLoader;

public class when_loading_errors_with_information_diagnostics : given.a_source_with_notes
{
    InvalidSemanticModel _failure = null!;

    void Establish() => File.WriteAllText(_path, File.ReadAllText(_path).Replace("produces ProjectRegistered", "produces UnknownEvent", StringComparison.Ordinal));

    async Task Because()
    {
        _result = await SemanticModelLoader.LoadAsync(_root, [], null, "Projects");
        _failure = (InvalidSemanticModel)await Catch.Exception(() => SemanticModelLoader.LoadFromPathAsync(_root));
    }

    [Fact] void should_refuse_the_model() => _result.Success.ShouldBeFalse();
    [Fact] void should_keep_the_compilation_failure_header() => _result.Diagnostics[0].Code.ShouldEqual("STAGE-PLAN-003");
    [Fact] void should_report_only_the_header_and_one_error() => _result.Diagnostics.Length.ShouldEqual(2);
    [Fact] void should_report_only_error_severity_as_blocking() => _result.Diagnostics.All(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ShouldBeTrue();
    [Fact] void should_keep_both_information_notes_separately() => _result.Notes.Count(note => note.Severity == DiagnosticSeverity.Information).ShouldEqual(2);
    [Fact] void should_throw_only_the_error_without_the_header() => _failure.Diagnostics.Count.ShouldEqual(1);
    [Fact] void should_keep_information_out_of_the_exception_message() => _result.Notes.Any(note => _failure.Message.Contains(note.Message, StringComparison.Ordinal)).ShouldBeFalse();
    [Fact] void should_keep_the_unknown_reference_warning_separately() => _result.Notes.Count(note => note.Severity == DiagnosticSeverity.Warning).ShouldEqual(1);
    [Fact] void should_keep_notes_on_the_exception() => _failure.Notes.Length.ShouldEqual(3);
}
#endif
