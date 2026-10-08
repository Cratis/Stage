// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Scene.for_ScreenDirectiveConverter.when_converting;

public class and_a_guarded_action_has_no_fallback : given.a_guarded_action
{
    Exception? _error;

    void Because() => _error = Catch.Exception(() => ScreenDirectiveConverter.Convert([_action], "InvoiceDetails"));

    [Fact] void should_refuse_instead_of_emitting_an_unknown_component() => _error.ShouldBeOfExactType<UnsupportedGuardedScreenAction>();
    [Fact] void should_identify_the_authored_action() => ((UnsupportedGuardedScreenAction)_error!).Label.ShouldEqual(_action.Label);
    [Fact] void should_preserve_the_source_location() => ((UnsupportedGuardedScreenAction)_error!).Location.ShouldEqual(_location);
    [Fact] void should_report_the_stable_diagnostic_code() => _error!.Message.ShouldContain(UnsupportedGuardedScreenAction.DiagnosticCode);
}
