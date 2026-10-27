// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Screenplay;
using Cratis.Stage.Rendering.Cratis.Emission;
using Cratis.Stage.Rendering.Cratis.Renderers;
using Xunit;
using InvalidEventModel = Cratis.Stage.Contracts.InvalidEventModel;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRenderer;

public class when_refusing_explicit_action_subjects : given.a_multi_slice_application
{
    [Theory]
    [InlineData(true, ScreenActionSubjectKind.Item)]
    [InlineData(true, ScreenActionSubjectKind.Screen)]
    [InlineData(false, ScreenActionSubjectKind.Item)]
    [InlineData(false, ScreenActionSubjectKind.Screen)]
    public async Task should_refuse_before_writing_any_output(bool toolbar, ScreenActionSubjectKind subject)
    {
        var application = WithAction(toolbar, subject);
        var expected = Assert.Throws<InvalidEventModel>(() => ScreenplayEventModelVisitor.EnsureBehaviorSupported(application));
        var output = new InMemoryCodeOutput();
        var renderer = Renderer(output);
        var error = await Catch.Exception(() => renderer.Render([application], new DirectoryInfo("unused-output"), TextWriter.Null, TextWriter.Null));
        var refusal = Assert.IsType<InvalidEventModel>(Assert.IsType<RenderingFailed>(error).Failures.Single());
        Assert.Equal(expected.Message, refusal.Message);
        refusal.Message.ShouldContain("STAGE-ESM-058:");
        refusal.Message.ShouldContain(toolbar ? "Toolbar item 'register'" : "Screen action 'RegisterInvoice'");
        refusal.Message.ShouldContain("Subjects.play");
        refusal.Message.ShouldContain("Line = 7, Column = 11");
        output.Files.ShouldBeEmpty();
        output.FailureMarkerWasWritten.ShouldBeFalse();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task should_leave_omitted_subject_output_byte_identical(bool toolbar)
    {
        var output = new InMemoryCodeOutput();
        var renderer = Renderer(output);
        await renderer.Render([WithAction(toolbar, null, includeAction: false)], new DirectoryInfo("unused-output"), TextWriter.Null, TextWriter.Null);
        var before = output.Files.ToArray();
        Assert.NotEmpty(before);
        await renderer.Render([WithAction(toolbar, null)], new DirectoryInfo("unused-output"), TextWriter.Null, TextWriter.Null);
        var after = output.Files.Skip(before.Length).ToArray();
        Assert.Equal(before.Select(file => file.RelativePath), after.Select(file => file.RelativePath));
        Assert.Equal(before.Length, after.Length);
        for (var index = 0; index < before.Length; index++)
        {
            Assert.Equal(Encoding.UTF8.GetBytes(before[index].Content), Encoding.UTF8.GetBytes(after[index].Content));
        }
    }

    ApplicationSyntax WithAction(bool toolbar, ScreenActionSubjectKind? subject, bool includeAction = true)
    {
        var module = _application.Modules.Single();
        var original = module.Features.Single();
        var location = new SourceLocation(7, 11, "Subjects.play");
        ScreenDirectiveSyntax directive = toolbar
            ? new ScreenToolbarSyntax("main", [new ToolbarItemSyntax("register", ToolbarItemKind.Action, "RegisterInvoice", location) { Subject = subject }], location)
            : new ScreenActionSyntax("RegisterInvoice", null, null, location) { Subject = subject };
        var slice = original.Slices.First() with { Screens = [new ScreenSyntax("Orders", null, includeAction ? [directive] : [], location)] };
        var feature = original with { Slices = [slice, .. original.Slices.Skip(1)] };
        return _application with { Modules = [module with { Features = [feature] }] };
    }

    CratisRenderer Renderer(InMemoryCodeOutput output) => new(
        _scaffolder,
        new Dictionary<SliceType, ISliceRenderer>
        {
            [SliceType.StateChange] = new StateChangeSliceRenderer(),
            [SliceType.StateView] = new StateViewSliceRenderer()
        },
        output);
}
