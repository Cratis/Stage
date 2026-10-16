// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Scene.Model.Screens;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Scene.for_ScreenTemplateConverter.when_converting;

/// <summary>
/// Screenplay accepts a screen template that declares <c language="screenplay">scopes application</c> and fits no slot as a
/// <c language="screenplay">root template</c> (PLAY0794); Scene's shell resolver takes an own screen template only with the root role.
/// </summary>
public class and_the_template_scopes_application_and_fits_no_slot : Specification
{
    ScreenTemplate _shell = null!;
    ScreenTemplate _page = null!;
    ScreenTemplate _slotted = null!;

    void Because()
    {
        _shell = ScreenTemplateConverter.Convert(Template("Docked", fitsSlot: null, scopes: ["application"]));
        _page = ScreenTemplateConverter.Convert(Template("Sheet", fitsSlot: null, scopes: ["feature"]));
        _slotted = ScreenTemplateConverter.Convert(Template("Inset", fitsSlot: "content", scopes: ["application"]));
    }

    static ScreenTemplateSyntax Template(string name, string? fitsSlot, string[] scopes) => new(name, [new SlotSyntax("content", null, SourceLocation.Start)], SourceLocation.Start)
    {
        FitsSlot = fitsSlot,
        RestrictsScopes = true,
        Scopes = scopes
    };

    [Fact] void should_take_the_root_role() => _shell.Metadata!.Role.ShouldEqual(TemplateRole.Root);
    [Fact] void should_keep_the_scope() => _shell.Metadata!.Scopes!.ShouldContainOnly([TemplateScope.Application]);
    [Fact] void should_not_give_a_page_template_the_root_role() => _page.Metadata!.Role.ShouldBeNull();
    [Fact] void should_not_give_a_template_that_fits_a_slot_the_root_role() => _slotted.Metadata!.Role.ShouldBeNull();
}
