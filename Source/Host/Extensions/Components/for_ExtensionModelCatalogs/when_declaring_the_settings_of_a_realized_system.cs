// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;
using AnApplication = Cratis.Stage.Host.Extensions.given.an_extension_application;

namespace Cratis.Stage.Host.Extensions.Components.for_ExtensionModelCatalogs;

public class when_declaring_the_settings_of_a_realized_system : AnApplication
{
    IReadOnlyList<SettingDeclaration> _settings = null!;

    async Task Because()
    {
        await LoadApplication();
        _settings = ExtensionModelCatalogs.Settings(_loaded.Model.Application, _modelPackages);
    }

    [Fact] void should_declare_each_setting_of_the_contract() => _settings.Select(setting => setting.Name).ShouldContainOnly("channel", "token");
    [Fact] void should_declare_a_public_setting_as_one_a_browser_may_read() => _settings.Single(setting => setting.Name == "channel").IsSecret.ShouldBeFalse();
    [Fact] void should_declare_a_secret_as_one_a_browser_may_not_read() => _settings.Single(setting => setting.Name == "token").IsSecret.ShouldBeTrue();
}
