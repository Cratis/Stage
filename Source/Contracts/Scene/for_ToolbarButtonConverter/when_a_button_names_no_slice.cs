// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Scene.Engine.Toolbar;
using Cratis.Scene.Model.Elements;
using Cratis.Scene.Model.Toolbar;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Scene.for_ToolbarButtonConverter;

public class when_a_button_names_no_slice : given.a_translated_application
{
    const string Source = "module Shop\n  feature Orders\n    slice StateChange PlaceOrder\n      command PlaceOrder\n        orderId Uuid identifier\n        produces OrderPlaced\n          orderId = orderId\n      event OrderPlaced\n        orderId Uuid\n    slice StateView OrderList\n      readmodel OrderRow\n        orderId Uuid\n      screen List\n        title \"Orders\"\n        toolbar actions\n          item legacy action PlaceOrder\n            label \"Legacy\"\n          button b title \"B\"\n    slice Automation Notify\n      reaction Notifier\n        when OrderPlaced\n  feature Returns\n    slice StateChange StartReturn\n      command StartReturn\n        returnId Uuid identifier\n        produces ReturnStarted\n          returnId = returnId\n      event ReturnStarted\n        returnId Uuid\n";

    IReadOnlyList<RenderFinding> _findings = null!;

    void Because()
    {
        TranslateRejected(Source);
        _findings = [.. RenderFindings.ForStructure(_application)];
    }

    [Fact] void should_report_one_finding() => _findings.Count.ShouldEqual(1);
    [Fact] void should_report_it_as_an_invalid_toolbar_button() => _findings[0].Kind.ShouldEqual(RenderFindingKind.ToolbarButtonInvalid);
    [Fact] void should_use_the_scene_diagnostic() => _findings[0].Message.ShouldContain("ToolbarButtonSliceEmpty");
    [Fact] void should_name_the_button() => _findings[0].Subject.ShouldContain("b");
    [Fact] void should_be_the_verdict_the_screenplay_compiler_reached() => _diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain("PLAY0763");
}
