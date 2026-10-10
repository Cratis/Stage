// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Printing;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Specifications;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Specifications.for_SpecificationFixtureText;

public class when_formatting_concrete_values : Specification
{
    ExpressionSyntax[] _values = [];
    string[] _formatted = [];

    void Establish() => _values =
    [
        new LiteralExpressionSyntax(null, SourceLocation.Start),
        new LiteralExpressionSyntax(true, SourceLocation.Start),
        new LiteralExpressionSyntax(false, SourceLocation.Start),
        new LiteralExpressionSyntax("quotes \" and backslash \\ and tabs\t and lines\n\r", SourceLocation.Start),
        new LiteralExpressionSyntax("æ <tag> & text", SourceLocation.Start),
        new LiteralExpressionSyntax(123.5, SourceLocation.Start),
        new LiteralExpressionSyntax(-1e-20, SourceLocation.Start),
        new LiteralExpressionSyntax(1e30, SourceLocation.Start),
        new LiteralExpressionSyntax((double)long.MinValue, SourceLocation.Start),
        new PathExpressionSyntax("Status.Active", SourceLocation.Start),
        new ListExpressionSyntax([new LiteralExpressionSyntax("æ \"\n", SourceLocation.Start), new LiteralExpressionSyntax(2d, SourceLocation.Start)], SourceLocation.Start),
        new ObjectExpressionSyntax([new("a\"key", new ListExpressionSyntax([new LiteralExpressionSyntax(false, SourceLocation.Start)], SourceLocation.Start), SourceLocation.Start)], SourceLocation.Start)
    ];

    void Because() => _formatted = [.. _values.Select(SpecificationFixtureText.Expression)];

    [Fact]
    void should_match_screenplays_fixture_printer()
    {
        var printer = new ScreenplayPrinter();
        for (var index = 0; index < _values.Length; index++)
        {
            var specification = new SpecificationSyntax("Fixture", [], new("Record", [new("value", _values[index], SourceLocation.Start)], SourceLocation.Start), [], [], SourceLocation.Start);
            printer.Print(specification).ShouldContain($"value = {_formatted[index]}\n");
        }
    }
}
