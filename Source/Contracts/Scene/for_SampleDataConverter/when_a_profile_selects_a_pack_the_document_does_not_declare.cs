// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Scene.for_SampleDataConverter;

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
