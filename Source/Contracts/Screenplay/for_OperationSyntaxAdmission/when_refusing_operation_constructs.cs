// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Screenplay.for_OperationSyntaxAdmission;

public class when_refusing_operation_constructs : Specification
{
    const string Prefix = "module M\n  feature F\n    slice StateChange S\n";

    [Theory]
    [InlineData("system Mailer\n", "System 'Mailer'")]
    [InlineData(Prefix + "      operation Send\n        uses Mailer\n", "Operation 'Send'")]
    [InlineData(Prefix + "      command C\n        produces operation Send\n          uses Mailer\n", "Inline operation 'Send'")]
    [InlineData(Prefix + "      operation Send\n        uses Mailer\n      command C\n        produces Send\n", "Operation 'Send'")]
    [InlineData(Prefix + "      operation Send\n        uses Mailer\n        execute\n          file Adapters/Send.cs\n", "Operation 'Send'")]
    [InlineData(Prefix + "      operation Send\n        uses Mailer\n        execute\n          ```csharp\n          return;\n          ```\n", "Operation 'Send'")]
    [InlineData(Prefix + "      specification T\n        given operation Send fails\n", "Operation failure fixture 'Send'")]
    [InlineData(Prefix + "      specification T\n        then operation Send\n", "Operation assertion 'Send'")]
    [InlineData(Prefix + "      specification T\n        then compensated Send\n", "Compensation assertion 'Send'")]
    public void should_refuse_before_legacy_translation(string source, string construct)
    {
        var parsed = new ScreenplayCompiler().Parse(source);
        Assert.True(parsed.Success, string.Join(Environment.NewLine, parsed.Diagnostics));
        var application = parsed.Value!;
        AssertRefusal(Catch.Exception(() => new OperationSyntaxAdmission().VisitApplication(application)), construct);
        AssertRefusal(Catch.Exception(() => new ScreenplayEventModelVisitor().Visit(application)), construct);
    }

    [Fact]
    public void should_preserve_the_authored_location()
    {
        var location = new SourceLocation(7, 9, "Operations.play");
        var phase = new OperationPhaseSyntax(null, null, null, null, location);
        var error = Assert.IsType<InvalidEventModel>(Catch.Exception(() => new OperationSyntaxAdmission().VisitNode(phase)));
        AssertRefusal(error, "Operation phase");
        error.Message.ShouldContain($"at {location} cannot be rendered yet");
        error.Message.ShouldContain("The event model at 'Operations.play'");
    }

    [Fact]
    public void should_admit_private_local_events() => Catch.Exception(() => new OperationSyntaxAdmission().VisitNode(new EventSyntax("Recorded", [], SourceLocation.Start))).ShouldBeNull();

    static void AssertRefusal(Exception? error, string construct)
    {
        var refusal = Assert.IsType<InvalidEventModel>(error);
        refusal.Message.ShouldContain($"STAGE-ESM-057: {construct} at ");
        refusal.Message.ShouldContain("the legacy syntax path would drop it or render it as an event append (Cratis/Stage#176, Cratis/Screenplay#301).");
    }
}
