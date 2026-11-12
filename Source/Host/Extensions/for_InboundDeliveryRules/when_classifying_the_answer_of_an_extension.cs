// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Host.Extensions.Inbound;
using Xunit;

namespace Cratis.Stage.Host.Extensions.for_InboundDeliveryRules;

public class when_classifying_the_answer_of_an_extension : Specification
{
    [Fact] void should_deliver_on_ok() => InboundDeliveryRules.Classify(200).ShouldEqual(InboundOutcome.Delivered);
    [Fact] void should_deliver_on_accepted() => InboundDeliveryRules.Classify(202).ShouldEqual(InboundOutcome.Delivered);
    [Fact] void should_reject_an_unauthorized_request() => InboundDeliveryRules.Classify(401).ShouldEqual(InboundOutcome.Rejected);
    [Fact] void should_reject_a_forbidden_request() => InboundDeliveryRules.Classify(403).ShouldEqual(InboundOutcome.Rejected);
    [Fact] void should_reject_a_bad_request() => InboundDeliveryRules.Classify(400).ShouldEqual(InboundOutcome.Rejected);
    [Fact] void should_retry_a_timeout() => InboundDeliveryRules.Classify(408).ShouldEqual(InboundOutcome.Retry);
    [Fact] void should_retry_throttling() => InboundDeliveryRules.Classify(429).ShouldEqual(InboundOutcome.Retry);
    [Fact] void should_retry_a_server_error() => InboundDeliveryRules.Classify(500).ShouldEqual(InboundOutcome.Retry);
    [Fact] void should_retry_an_unavailable_extension() => InboundDeliveryRules.Classify(503).ShouldEqual(InboundOutcome.Retry);
    [Fact] void should_retry_what_is_not_a_final_answer() => InboundDeliveryRules.Classify(302).ShouldEqual(InboundOutcome.Retry);
}
