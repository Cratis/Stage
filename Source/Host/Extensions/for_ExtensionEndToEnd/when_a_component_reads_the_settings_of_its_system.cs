// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Host.Extensions.Components;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using AnExtensionHost = Cratis.Stage.Host.Extensions.given.an_extension_host;

namespace Cratis.Stage.Host.Extensions.for_ExtensionEndToEnd;

public class when_a_component_reads_the_settings_of_its_system : AnExtensionHost
{
    IReadOnlyDictionary<string, string> _settings = null!;

    async Task Because()
    {
        await StartHost();
        _settings = Services.GetRequiredService<ExtensionPublicSettings>().Of("Notifier");
    }

    [Fact] void should_get_the_setting_the_contract_exposes_as_public() => _settings["channel"].ShouldEqual("ops");
    [Fact] void should_not_get_the_secret_even_though_it_has_a_value() => _settings.Keys.ShouldContainOnly("channel");
}
