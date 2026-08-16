// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Scene.Model.Elements;
using Cratis.Scene.Model.Layouts;
using Cratis.Scene.Model.Packages;
using Cratis.Scene.Model.Profiles;
using Cratis.Scene.Model.Screens;
using Cratis.Scene.Model.SizeClasses;
using Cratis.Specifications;

namespace Cratis.Stage.Contracts.Scene.for_RenderPlanner.given;

public class an_application_targeting_web_and_ios : Specification
{
    protected SceneApplication _application = null!;
    protected IReadOnlyList<ScenePackage> _catalog = null!;

    void Establish()
    {
        _catalog =
        [
            new ScenePackage("core", "1.0.0", PackageKind.ComponentLibrary, [], ["core:title", "core:table"], [], [], [], []),
            new ScenePackage("Tailwind", "3.0.0", PackageKind.Styling, [], [], [], [], [], []),
            new ScenePackage("PrimeReact", "1.2.0", PackageKind.ComponentLibrary, [new PackageDependency("Tailwind")], ["core:table"], [], [], [], ["Aurora"]),
        ];

        // The shell reflows from a row to a column on a narrow target, which is what makes the two targets
        // resolve to different arrangements from the same declaration.
        var layout = new Layout(
            "AppShell",
            [new Slot("navigation"), new Slot("content")],
            new FlowArrangement(
                new FlowRow { Children = [new FlowSlotLeaf("navigation"), new FlowSlotLeaf("content")] },
                [new FlowOverride(WidthSizeClass.Compact, null, new FlowColumn { Children = [new FlowSlotLeaf("navigation"), new FlowSlotLeaf("content")] })]));

        var screenTemplate = new ScreenTemplate("MasterDetail", "content", [new Slot("sidebar"), new Slot("main")]);

        var screen = new Screen(
            "InvoiceDetails",
            "AppShell",
            new Dictionary<string, IReadOnlyList<SceneElement>>(StringComparer.Ordinal)
            {
                ["main"] =
                [
                    SceneElementFactory.Component(
                        "InvoiceDetails.main.0-title",
                        "core:title",
                        slots: new Dictionary<string, IReadOnlyList<SceneElement>>(StringComparer.Ordinal)
                        {
                            ["content"] = [SceneElementFactory.Component("InvoiceDetails.main.0-title.0-table", "core:table")],
                        }),
                ],
            },
            [],
            [],
            "MasterDetail");

        _application = new SceneApplication(
            [
                new UiProfile("Admin", "web", ["PrimeReact"], null, "AppShell", "Aurora"),
                new UiProfile("Admin", "ios", ["PrimeReact"], new SizeClass(WidthSizeClass.Compact, HeightSizeClass.Regular), "AppShell", "Aurora"),
            ],
            [new Theme("Aurora", ["core", "Tailwind", "PrimeReact"])],
            [layout],
            [screenTemplate],
            [],
            [screen]);
    }
}
