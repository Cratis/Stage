// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Scene.Model.SampleData;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Scene.for_SampleDataConverter;

public class when_converting_sample_data : given.a_translated_application
{
    const string Source =
        """
        sample data NordicDemo
          title "CRM · Nordic SMB"
          locale nb-NO
          shape Customer
            row
              name = "Fjordline AS"
              seats = 12
              score = 4.5
              active = true
              note = null
            row
              name = "Nordlys"

        sample data HelpdeskDemo
          shape Ticket
            row
              subject = "Printer"

        module Crm
          feature Customers
            slice StateView CustomerList
              readmodel Customer
                name String
                seats Int
                score Decimal
                active Bool
                note String optional
              readmodel Account
                name String
                seats Int
                score Decimal
                active Bool
                note String optional
            slice StateView TicketList
              readmodel Ticket
                subject String

        ui profile Desktop
          target platform web
          packages
            core
          sample data
            active NordicDemo
            shape Customer maps to Account
            bind Ticket to HelpdeskDemo

        ui profile Phone
          target platform ios
          packages
            core
          sample data
            active blueprint crm-nordic-smb
        """;

    SampleDataPack Nordic => _application.SampleDataPacks.Single(pack => pack.Id == "nordic-demo");

    SampleDataSelection Selection(string profile) => _application.UiProfiles.Single(candidate => candidate.Name == profile).SampleData!;

    void Because() => Translate(Source);

    [Fact] void should_convert_every_pack() => _application.SampleDataPacks.Select(pack => pack.Id).ShouldContainOnly("nordic-demo", "helpdesk-demo");
    [Fact] void should_derive_a_lower_case_id_from_the_pack_name() => SampleDataConverter.IdOf("NordicSMBDemo2").ShouldEqual("nordic-smb-demo2");
    [Fact] void should_carry_the_title() => Nordic.Title.ShouldEqual("CRM · Nordic SMB");
    [Fact] void should_carry_the_locale() => Nordic.Locale.ShouldEqual("nb-NO");
    [Fact] void should_own_the_pack_as_the_user() => Nordic.Owner.ShouldEqual("user");
    [Fact] void should_show_a_pack_without_a_title_by_its_name() => _application.SampleDataPacks.Single(pack => pack.Id == "helpdesk-demo").Title.ShouldEqual("HelpdeskDemo");
    [Fact] void should_follow_the_default_locale_without_one() => _application.SampleDataPacks.Single(pack => pack.Id == "helpdesk-demo").Locale.ShouldEqual("en-GB");
    [Fact] void should_convert_the_sets_and_rows() => (Nordic.Sets.Single().Shape, Nordic.Sets.Single().Rows.Count).ShouldEqual(("Customer", 2));
    [Fact] void should_keep_a_string_as_text() => Nordic.Sets[0].Rows[0]["name"]!.GetValue<string>().ShouldEqual("Fjordline AS");
    [Fact] void should_keep_a_whole_number_whole() => Nordic.Sets[0].Rows[0]["seats"]!.GetValue<long>().ShouldEqual(12L);
    [Fact] void should_keep_a_decimal_a_decimal() => Nordic.Sets[0].Rows[0]["score"]!.GetValue<decimal>().ShouldEqual(4.5m);
    [Fact] void should_keep_a_flag_a_boolean() => Nordic.Sets[0].Rows[0]["active"]!.GetValue<bool>().ShouldBeTrue();
    [Fact] void should_keep_null_null() => Nordic.Sets[0].Rows[0]["note"].ShouldBeNull();
    [Fact] void should_keep_the_authored_field_order() => Nordic.Sets[0].Rows[0].Select(field => field.Key).ShouldEqual(["name", "seats", "score", "active", "note"]);
    [Fact] void should_choose_the_active_pack_in_the_profile() => Selection("Desktop").ActivePack.ShouldEqual("nordic-demo");
    [Fact] void should_convert_a_shape_mapping() => Selection("Desktop").Mappings.Single().ShouldEqual(new SampleDataMapping("Customer", "Account"));
    [Fact] void should_convert_a_binding_to_the_derived_pack_id() => Selection("Desktop").Bindings.Single().ShouldEqual(new SampleDataBinding("Ticket", "helpdesk-demo"));
    [Fact] void should_keep_a_blueprint_pack_id_as_written() => Selection("Phone").ActivePack.ShouldEqual("crm-nordic-smb");
    [Fact] void should_record_the_blueprint_packs_the_build_resolves() => _application.BlueprintSampleDataPacks.ShouldContainOnly("crm-nordic-smb");
    [Fact] void should_report_no_findings() => RenderFindings.ForStructure(_application).ShouldBeEmpty();
}

public class when_a_document_declares_no_sample_data : given.a_translated_application
{
    void Because() => Translate(Model);

    [Fact] void should_have_no_packs() => _application.SampleDataPacks.ShouldBeEmpty();
    [Fact] void should_have_no_blueprint_packs() => _application.BlueprintSampleDataPacks.ShouldBeEmpty();
}

public class when_two_packs_derive_the_same_scene_id : given.a_translated_application
{
    const string Source =
        """
        sample data NordicDemo
          shape Customer
            row
              name = "A"

        sample data nordicDemo
          shape Customer
            row
              name = "B"

        module Crm
          feature Customers
            slice StateView CustomerList
              readmodel Customer
                name String
        """;

    IReadOnlyList<RenderFinding> _findings = null!;

    void Because()
    {
        Translate(Source);
        _findings = [.. RenderFindings.ForStructure(_application)];
    }

    [Fact] void should_report_one_finding() => _findings.Count.ShouldEqual(1);
    [Fact] void should_report_it_as_invalid_sample_data() => _findings[0].Kind.ShouldEqual(RenderFindingKind.SampleDataInvalid);
    [Fact] void should_say_the_ids_collide() => _findings[0].Message.ShouldContain("derive the same id");
}

public class when_a_profile_selects_a_pack_the_document_does_not_declare : given.a_translated_application
{
    const string Source =
        """
        module Crm
          feature Customers
            slice StateView CustomerList
              readmodel Customer
                name String

        ui profile Desktop
          target platform web
          packages
            core
          sample data
            active Ghost
        """;

    IReadOnlyList<RenderFinding> _findings = null!;

    void Because()
    {
        TranslateRejected(Source);
        _findings = [.. RenderFindings.ForStructure(_application)];
    }

    [Fact] void should_report_one_finding() => _findings.Count.ShouldEqual(1);
    [Fact] void should_report_it_as_invalid_sample_data() => _findings[0].Kind.ShouldEqual(RenderFindingKind.SampleDataInvalid);
    [Fact] void should_name_the_pack() => _findings[0].Message.ShouldContain("'ghost'");
    [Fact] void should_be_the_verdict_the_screenplay_compiler_reached() => _diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain("PLAY0714");
}
