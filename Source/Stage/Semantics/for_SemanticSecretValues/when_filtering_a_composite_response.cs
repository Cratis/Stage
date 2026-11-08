// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Semantics.for_SemanticSecretValues;

public class when_filtering_a_composite_response : given.a_composite_response_application
{
    Dictionary<string, object?> _scalarResult = null!;
    Dictionary<string, object?> _recordResult = null!;

    void Because()
    {
        _scalarResult = (Dictionary<string, object?>)SemanticSecretValues.Response(Credentials("s3cret"), _scalar.Response, _application)!;
        _recordResult = (Dictionary<string, object?>)SemanticSecretValues.Response(
            new Dictionary<string, object?>
            {
                ["name"] = "n",
                ["credentials"] = Credentials("s3cret")
            },
            _record.Response,
            _application)!;
    }

    [Fact] void should_omit_the_secret_of_a_composite_return() => _scalarResult.ContainsKey("password").ShouldBeFalse();
    [Fact] void should_keep_the_other_members_of_a_composite_return() => _scalarResult["note"].ShouldEqual("main");
    [Fact] void should_omit_the_secret_of_a_composite_record_field() => ((Dictionary<string, object?>)_recordResult["credentials"]!).ContainsKey("password").ShouldBeFalse();
    [Fact] void should_keep_the_other_members_of_a_composite_record_field() => ((Dictionary<string, object?>)_recordResult["credentials"]!)["note"].ShouldEqual("main");
    [Fact] void should_keep_the_other_fields() => _recordResult["name"].ShouldEqual("n");
}
