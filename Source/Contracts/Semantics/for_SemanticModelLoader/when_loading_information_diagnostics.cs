// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Diagnostics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Semantics.for_SemanticModelLoader;

public class when_loading_information_diagnostics : given.a_source_with_notes
{
    async Task Because() => _result = await SemanticModelLoader.LoadAsync(_root, [], null, "Projects");

    [Fact] void should_load_successfully() => _result.Success.ShouldBeTrue();
    [Fact] void should_not_report_blocking_diagnostics() => _result.Errors.ShouldBeEmpty();
    [Fact] void should_keep_all_diagnostics_for_existing_consumers() => _result.Diagnostics.SequenceEqual(_result.Notes).ShouldBeTrue();
    [Fact] void should_keep_both_information_notes() => _result.Notes.Length.ShouldEqual(2);
    [Fact] void should_preserve_information_severity() => _result.Notes.All(note => note.Severity == DiagnosticSeverity.Information).ShouldBeTrue();
    [Fact] void should_preserve_codes() => _result.Notes.All(note => note.Code == "PLAY0270").ShouldBeTrue();
    [Fact] void should_preserve_messages() => _result.Notes.All(note => note.Message.Length > 0).ShouldBeTrue();
    [Fact] void should_preserve_relative_locations() => _result.Notes.All(note => note.Source!.StartsWith("nested/source.play(", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_keep_notes_on_the_loaded_model() => _result.Loaded!.Notes.SequenceEqual(_result.Notes).ShouldBeTrue();
}
#endif
