// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Projections;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.Expressions;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CommandContextAccess;

public class when_the_path_is_out_of_the_handler_s_reach : Specification
{
    readonly List<string> _diagnostics = [];
    CommandContextAccess _access = null!;
    string _unnamed = null!;
    string _eventContext = null!;
    string _eventSourceId = null!;

    void Establish() => _access = new CommandContextAccess("Command 'RegisterInvoice'", _diagnostics);

    void Because()
    {
        _unnamed = _access.Render(new ContextExpressionSyntax("weather.today", SourceLocation.Start));
        _eventContext = _access.Render(new EventContextExpressionSyntax("occurred", SourceLocation.Start));
        _eventSourceId = _access.RenderEventSourceId();
    }

    [Fact] void should_render_a_path_the_language_does_not_name_as_a_missing_value() => _unnamed.ShouldEqual("default!");
    [Fact] void should_render_an_event_context_read_as_a_missing_value() => _eventContext.ShouldEqual("default!");
    [Fact] void should_render_an_event_source_id_read_as_a_missing_value() => _eventSourceId.ShouldEqual("default!");
    [Fact] void should_report_every_one_of_them() => _diagnostics.Count.ShouldEqual(3);
    [Fact] void should_say_which_command_could_not_reach_it() =>
        _diagnostics.ShouldContain("Command 'RegisterInvoice' reads '$context.weather.today', which the rendered handler cannot reach — the language names no such value; rendered as a missing value.");
    [Fact] void should_ask_for_no_collaborator_it_cannot_use() => _access.Collaborators.ShouldBeEmpty();
}
