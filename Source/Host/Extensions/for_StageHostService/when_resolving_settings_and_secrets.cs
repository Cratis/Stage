// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Extensions.Protocol;
using Cratis.Stage.Host.Extensions.given;
using Xunit;
using RequestContext = Cratis.Stage.Extensions.Protocol.RequestContext;

namespace Cratis.Stage.Host.Extensions.for_StageHostService;

public class when_resolving_settings_and_secrets : an_extension_protocol
{
    const string SecretValue = "xoxb-very-secret";

    GetSettingResponse _setting = null!;
    GetSettingResponse _unset = null!;
    GetSettingResponse _secretAsSetting = null!;
    GetSettingResponse _unknownSetting = null!;
    GetSecretResponse _secret = null!;
    GetSecretResponse _settingAsSecret = null!;
    GetSecretResponse _unknownSecret = null!;
    GetSecretResponse _secretNotSet = null!;

    async Task Establish()
    {
        await StartBackend();
        await StartStage();
        _backend.Description.Settings.Add(new SettingDeclaration { Name = "channel", Required = true });
        _backend.Description.Settings.Add(new SettingDeclaration { Name = "region" });
        _backend.Description.Settings.Add(new SettingDeclaration { Name = "token", Secret = true, Required = true });
        _backend.Description.Settings.Add(new SettingDeclaration { Name = "signingKey", Secret = true });
        _values["Slack:channel"] = "#orders";
        _values["Slack:token"] = SecretValue;
    }

    async Task Because()
    {
        var host = await Connected();
        _setting = await host.GetSettingAsync(new GetSettingRequest { Name = "channel", Context = new RequestContext { InvocationId = "invocation-1" } });
        _unset = await host.GetSettingAsync(new GetSettingRequest { Name = "region" });
        _secretAsSetting = await host.GetSettingAsync(new GetSettingRequest { Name = "token" });
        _unknownSetting = await host.GetSettingAsync(new GetSettingRequest { Name = "nothing" });
        _secret = await host.GetSecretAsync(new GetSecretRequest { Name = "token" });
        _settingAsSecret = await host.GetSecretAsync(new GetSecretRequest { Name = "channel" });
        _unknownSecret = await host.GetSecretAsync(new GetSecretRequest { Name = "nothing" });
        _secretNotSet = await host.GetSecretAsync(new GetSecretRequest { Name = "signingKey" });
    }

    [Fact] void should_find_a_setting() => (_setting.Status == GetSettingResponse.Types.Status.Found && _setting.Value == "#orders").ShouldBeTrue();
    [Fact] void should_say_a_declared_setting_has_no_value() => _unset.Status.ShouldEqual(GetSettingResponse.Types.Status.NotSet);
    [Fact] void should_not_deliver_a_secret_as_a_setting() => (_secretAsSetting.Status == GetSettingResponse.Types.Status.IsSecret && _secretAsSetting.Value.Length == 0).ShouldBeTrue();
    [Fact] void should_not_know_an_undeclared_setting() => _unknownSetting.Status.ShouldEqual(GetSettingResponse.Types.Status.Unknown);
    [Fact] void should_deliver_a_secret() => (_secret.Status == GetSecretResponse.Types.Status.Found && _secret.Value == SecretValue).ShouldBeTrue();
    [Fact] void should_not_deliver_a_setting_as_a_secret() => (_settingAsSecret.Status == GetSecretResponse.Types.Status.NotSecret && _settingAsSecret.Value.Length == 0).ShouldBeTrue();
    [Fact] void should_not_know_an_undeclared_secret() => _unknownSecret.Status.ShouldEqual(GetSecretResponse.Types.Status.Unknown);
    [Fact] void should_say_a_declared_secret_has_no_value() => _secretNotSet.Status.ShouldEqual(GetSecretResponse.Types.Status.NotSet);
    [Fact] void should_never_log_the_secret() => _logs.Records.Exists(record => record.Message.Contains(SecretValue) || record.Scope.Values.Any(value => value?.ToString()?.Contains(SecretValue) == true)).ShouldBeFalse();
}
