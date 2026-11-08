// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_StageJourneyFile;

public class when_parsing_text_that_is_not_json : Specification
{
    StageJourneyFileParse _parse = null!;

    void Because() => _parse = StageJourneyFile.Parse("{ \"schema\": ");

    [Fact] void should_not_produce_a_file() => _parse.File.ShouldBeNull();
    [Fact] void should_report_invalid_json() => _parse.Errors.Single().StartsWith("$: invalid JSON:", StringComparison.Ordinal).ShouldBeTrue();
}
