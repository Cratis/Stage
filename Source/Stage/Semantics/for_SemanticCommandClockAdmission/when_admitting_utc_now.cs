// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Xunit;

namespace Cratis.Stage.Semantics.for_SemanticCommandClockAdmission;

public class when_admitting_utc_now
{
    [Theory]
    [InlineData("at == now")]
    [InlineData("at != now")]
    [InlineData("now < at")]
    [InlineData("at <= now")]
    [InlineData("at > now - 1 day")]
    [InlineData("at >= now and at < now + 1 hour")]
    public void should_admit_only_the_supported_owning_command(string condition)
    {
        var model = Compile(condition);
        SemanticRunAdmission.RunStanding(model, new HashSet<SemanticId>()).UnknownMembers.ShouldBeEmpty();
        var plan = SemanticExecutionPlan.Compile(model).Plan!;
        SemanticRunAdmission.CommandRangeRefusal(plan, plan.Commands.Values.Single()).ShouldBeNull();
    }

    [Theory]
    [InlineData("nonUtc")]
    [InlineData("configurable")]
    [InlineData("context")]
    [InlineData("calendar")]
    public void should_refuse_unsupported_clock_configuration_at_admission(string boundary)
    {
        var model = Compile("at >= now");
        var application = Blocked(model.Application, boundary);
        var changed = ExecutableSemanticModel.Create(model.LanguageVersion, model.SemanticVersion, application);
        SemanticRunAdmission.RunStanding(changed, new HashSet<SemanticId>()).UnknownMembers.ShouldNotBeEmpty();
        var plan = SemanticExecutionPlan.Compile(changed).Plan!;
        SemanticRunAdmission.CommandRangeRefusal(plan, plan.Commands.Values.Single()).ShouldNotBeNull();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void should_refuse_optional_and_collection_properties(bool collection)
    {
        var model = Compile("at >= now");
        var command = Command(model.Application);
        var properties = command.Properties.Select(property => property.Name == "at"
            ? property with { Type = property.Type with { IsOptional = !collection, IsCollection = collection } }
            : property).ToImmutableArray();
        var application = Replace(model.Application, command with { Properties = properties });
        SemanticCommandClockSupport.Supports(application, Command(application)).ShouldBeFalse();
        var rejection = Assert.Throws<InvalidSemanticContract>(() => ExecutableSemanticModel.Create(model.LanguageVersion, model.SemanticVersion, application));
        rejection.Message.ShouldEqual("Clock comparisons require required scalar command properties.");
    }

    [Theory]
    [InlineData("SemanticClockOperand.Future")]
    [InlineData("SemanticClockKind.Today")]
    [InlineData("SemanticConditionOperand.DateOf")]
    public void should_not_admit_other_members_by_clock_prefix(string member)
    {
        var model = Compile("at >= now");
        var slice = SemanticVersionFeatures.Slices(model.Application).Single();
        var unknown = new SemanticUnknownMember(slice.Id, "slice", member, "Application.Modules[0].Features[0].Slices[0].Commands[0].Requirements[0].Condition.Right.Clock", false);
        var standing = new EsmSupportStanding(EsmVersionStanding.Audited, [unknown]);
        SemanticCommandClockAdmission.Known(standing, model).UnknownMembers.Single().ShouldEqual(unknown);
    }

    [Fact]
    public void should_isolate_an_unsupported_sibling_clock_command()
    {
        var model = Compile("at >= now", sibling: true);
        var plan = SemanticExecutionPlan.Compile(model).Plan!;
        SemanticRunAdmission.CommandRangeRefusal(plan, plan.Commands.Values.Single(command => command.Name == "Keep")).ShouldBeNull();
        SemanticRunAdmission.CommandRangeRefusal(plan, plan.Commands.Values.Single(command => command.Name == "Unsupported")).ShouldNotBeNull();
    }

    static SemanticApplication Blocked(SemanticApplication application, string boundary) => boundary switch
    {
        "nonUtc" => application with { TimeZone = new("Europe/Oslo", false) },
        "configurable" => application with { TimeZone = new("UTC", true) },
        "context" => application with { ContextMembers = ["tenant"] },
        "calendar" => Replace(application, Command(application) with { Calendar = new("UTC", null) }),
        _ => throw new InvalidOperationException()
    };

    static SemanticCommand Command(SemanticApplication application) => SemanticVersionFeatures.Slices(application).Single().Commands.Single();

    static SemanticApplication Replace(SemanticApplication application, SemanticCommand command)
    {
        var module = application.Modules.Single();
        var feature = module.Features.Single();
        var slice = feature.Slices.Single();
        return application with { Modules = [module with { Features = [feature with { Slices = [slice with { Commands = [command] }] }] }] };
    }

    static ExecutableSemanticModel Compile(string condition, bool sibling = false)
    {
        var source = $$"""
            module Scheduling
              feature Windows
                slice StateChange Keep
                  command Keep
                    id String identifier
                    at DateTime
                    validate
                      require {{condition}}
                    produces Kept
                      for id
                  event Kept
            """;
        if (sibling)
        {
            source += "\n      command Unsupported\n        id String identifier\n        at DateTime\n        validate\n          at > now\n        produces Kept\n          for id";
        }
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("UtcAdmission"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("app"), "app", "application.play", source);
        var compilation = new SemanticModelCompiler().Compile("UtcAdmission", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join(';', compilation.Diagnostics));
        return compilation.Value.Model;
    }
}
