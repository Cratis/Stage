// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Projections;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.Expressions;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_ExpressionRenderer;

/// <summary>
/// The renderings that hold when Chronicle's <c>EventContext</c> is in scope as <c>context</c> — a reactor method
/// and a projection. What <c>$context</c> becomes elsewhere is the enclosing artifact's to say; a command handler
/// receives no such parameter, so its renderings are specified against <c>CommandContextAccess</c> instead.
/// </summary>
public class when_rendering_expressions : Specification
{
    string _stringLiteral = null!;
    string _boolLiteral = null!;
    string _numberLiteral = null!;
    string _nullLiteral = null!;
    string _path = null!;
    string _dottedPath = null!;
    string _context = null!;
    string _environment = null!;
    string _causedByWithProperty = null!;
    string _causedByWithoutProperty = null!;
    string _eventSourceId = null!;
    string _template = null!;

    void Because()
    {
        _stringLiteral = ExpressionRenderer.Render(new LiteralExpressionSyntax("Acme Corp", SourceLocation.Start));
        _boolLiteral = ExpressionRenderer.Render(new LiteralExpressionSyntax(true, SourceLocation.Start));
        _numberLiteral = ExpressionRenderer.Render(new LiteralExpressionSyntax(42, SourceLocation.Start));
        _nullLiteral = ExpressionRenderer.Render(new LiteralExpressionSyntax(null, SourceLocation.Start));
        _path = ExpressionRenderer.Render(new PathExpressionSyntax("invoiceId", SourceLocation.Start));
        _dottedPath = ExpressionRenderer.Render(new PathExpressionSyntax("billingContact.email", SourceLocation.Start));
        _context = ExpressionRenderer.Render(new ContextExpressionSyntax("identity.id", SourceLocation.Start));
        _environment = ExpressionRenderer.Render(new EnvironmentExpressionSyntax("SERVICE_NAME", SourceLocation.Start));
        _causedByWithProperty = ExpressionRenderer.Render(new CausedByExpressionSyntax("name", SourceLocation.Start));
        _causedByWithoutProperty = ExpressionRenderer.Render(new CausedByExpressionSyntax(null, SourceLocation.Start));
        _eventSourceId = ExpressionRenderer.Render(new EventSourceIdExpressionSyntax(SourceLocation.Start));
        _template = ExpressionRenderer.Render(new TemplateExpressionSyntax(
            [
                new TemplateTextSyntax("Invoice ", SourceLocation.Start),
                new TemplateInterpolationSyntax(new PathExpressionSyntax("invoiceNumber", SourceLocation.Start), SourceLocation.Start),
            ],
            SourceLocation.Start));
    }

    [Fact] void should_render_a_string_literal_as_a_quoted_string() => _stringLiteral.ShouldEqual("\"Acme Corp\"");
    [Fact] void should_render_a_bool_literal_as_a_c_sharp_keyword() => _boolLiteral.ShouldEqual("true");
    [Fact] void should_render_a_number_literal_invariantly() => _numberLiteral.ShouldEqual("42");
    [Fact] void should_render_a_null_literal_as_null() => _nullLiteral.ShouldEqual("null");
    [Fact] void should_render_a_path_as_a_pascal_case_property_reference() => _path.ShouldEqual("InvoiceId");
    [Fact] void should_render_a_dotted_path_segment_by_segment() => _dottedPath.ShouldEqual("BillingContact.Email");
    [Fact] void should_render_a_context_path_against_the_event_context() => _context.ShouldEqual("context.Identity.Id");
    [Fact] void should_render_an_environment_expression_as_environment_get_environment_variable() =>
        _environment.ShouldEqual("Environment.GetEnvironmentVariable(\"SERVICE_NAME\")");
    [Fact] void should_render_caused_by_with_a_property() => _causedByWithProperty.ShouldEqual("context.CausedBy.Name");
    [Fact] void should_render_caused_by_without_a_property() => _causedByWithoutProperty.ShouldEqual("context.CausedBy");
    [Fact] void should_render_event_source_id_against_the_event_context() => _eventSourceId.ShouldEqual("context.EventSourceId");
    [Fact] void should_render_a_template_as_an_interpolated_string() => _template.ShouldEqual("$\"Invoice {InvoiceNumber}\"");
}
