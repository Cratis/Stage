// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Specifications;
using Cratis.Stage.Host.Extensions.Inbound;
using Xunit;

namespace Cratis.Stage.Host.Extensions.for_InboundDeliveryRules;

public class when_identifying_a_request : Specification
{
    static readonly byte[] _body = Encoding.UTF8.GetBytes("{\"id\":\"evt_1\"}");

    string _same = null!;
    string _again = null!;
    string _otherBody = null!;
    string _otherPath = null!;
    string _otherMethod = null!;
    string _otherQuery = null!;
    string _otherSystem = null!;
    string _caseOfMethod = null!;

    void Because()
    {
        _same = InboundDeliveryRules.Identify("Payments", "POST", "/webhook", "a=1", _body);
        _again = InboundDeliveryRules.Identify("Payments", "POST", "/webhook", "a=1", _body);
        _otherBody = InboundDeliveryRules.Identify("Payments", "POST", "/webhook", "a=1", Encoding.UTF8.GetBytes("{\"id\":\"evt_2\"}"));
        _otherPath = InboundDeliveryRules.Identify("Payments", "POST", "/webhook/b", "a=1", _body);
        _otherMethod = InboundDeliveryRules.Identify("Payments", "PUT", "/webhook", "a=1", _body);
        _otherQuery = InboundDeliveryRules.Identify("Payments", "POST", "/webhook", "a=2", _body);
        _otherSystem = InboundDeliveryRules.Identify("Slack", "POST", "/webhook", "a=1", _body);
        _caseOfMethod = InboundDeliveryRules.Identify("Payments", "post", "/webhook", "a=1", _body);
    }

    [Fact] void should_be_the_same_for_the_same_request() => _again.ShouldEqual(_same);
    [Fact] void should_start_with_the_system() => _same.StartsWith("Payments/", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_differ_for_another_body() => _otherBody.ShouldNotEqual(_same);
    [Fact] void should_differ_for_another_path() => _otherPath.ShouldNotEqual(_same);
    [Fact] void should_differ_for_another_method() => _otherMethod.ShouldNotEqual(_same);
    [Fact] void should_differ_for_another_query() => _otherQuery.ShouldNotEqual(_same);
    [Fact] void should_differ_for_another_system() => _otherSystem.ShouldNotEqual(_same);
    [Fact] void should_not_depend_on_the_case_of_the_method() => _caseOfMethod.ShouldEqual(_same);
}
