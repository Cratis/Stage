// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Cratis.Stage.Rendering.Cratis.for_ArcScreenplayRoundTrip.given;

internal static class native_command_sources
{
    internal const string Wrapper = "new EventForEventSourceId(new EventSourceId((OtherId).ToString()), new AuthorRegistered(Name))";
    internal const string RoutedWrapper = "new EventForEventSourceId(ThirdId, new AuthorAudited(Name)) { EventSource = typeof(LedgerEventSource), EventStream = LedgerEventSource.Postings, EventStreamId = Month }";

    internal static string Identifier(string type, string identity, string? destination = null, bool block = false) => $$"""
        #nullable enable
        using System;
        using System.Globalization;
        using Cratis.Arc.Commands.ModelBound;
        using Cratis.Arc.Chronicle.Commands;
        using Cratis.Chronicle.Events;
        using Cratis.Chronicle.EventSequences;
        using Cratis.Concepts;
        namespace Banking.Authors.Registration;
        public record TextIdentity(string Value) : ConceptAs<string>(Value);
        public record NumberIdentity(long Value) : ConceptAs<long>(Value);
        public record UuidIdentity(Guid Value) : ConceptAs<Guid>(Value);
        public record SourceIdentity(Guid Value) : EventSourceId<Guid>(Value);
        [EventType] public record AuthorRegistered(string Name);
        [Command, EventSourceType("Account"), EventStreamType("Main")]
        public record RegisterAuthor({{type}} Id, {{type}} OtherId, string Name) : ICanProvideEventSourceId
        {
            public EventSourceId GetEventSourceId() {{(block ? "{ return " + identity + "; }" : "=> " + identity + ";")}}
            public {{(destination is null ? "AuthorRegistered" : "EventForEventSourceId")}} Handle() =>
                {{(destination is null ? "new(Name)" : "new EventForEventSourceId(" + destination + ", new AuthorRegistered(Name))")}};
        }
        """;

    internal static string Tuple(string handler) => $$"""
        using System;
        using System.Collections.Generic;
        using System.Threading.Tasks;
        using Cratis.Arc.Commands.ModelBound;
        using Cratis.Arc.Chronicle.Commands;
        using Cratis.Chronicle.Events;
        using Cratis.Chronicle.EventSequences;
        using Cratis.Chronicle.Keys;
        namespace Banking.Authors.Registration;
        [EventType] public record AuthorRegistered(string Name);
        [EventType] public record AuthorAudited(string Name);
        [Cratis.Chronicle.EventSources.EventSource("stored-ledger")]
        [Cratis.Chronicle.EventSources.EventStream(LedgerEventSource.Postings)]
        public class LedgerEventSource : Cratis.Chronicle.EventSources.IEventSource
        {
            public const string Postings = "stored-postings";
        }
        [Command, EventSourceType("Account"), EventStreamType("Main")]
        public record RegisterAuthor([Key] string Id, string OtherId, string ThirdId, string Month, string Name)
        {
            {{handler.Replace("WRAPPER", Wrapper, StringComparison.Ordinal).Replace("AUDIT", RoutedWrapper, StringComparison.Ordinal)}}
        }
        public record Receipt(string Name);
        public record ConvertedReceipt(string Name)
        {
            public static implicit operator Receipt(ConvertedReceipt value) => new(value.Name);
        }
        """;

    internal static string Generated(string signature, string expression, string extra = "", string additionalParameter = "", string key = "") => $$"""
        using System;
        using System.Threading.Tasks;
        using Cratis.Arc.Commands;
        using Cratis.Arc.Commands.ModelBound;
        using Cratis.Arc.Chronicle.Commands;
        using Cratis.Chronicle.Events;
        using Cratis.Chronicle.EventSequences;
        using Cratis.Concepts;
        namespace Banking.Authors.Registration;
        public record AuthorId(Guid Value) : EventSourceId<Guid>(Value), IGeneratable<AuthorId>
        {
            public static AuthorId New() => new(Guid.NewGuid());
        }
        [EventType] public record AuthorRegistered(string Name);
        [EventType] public record AuthorAudited(string Name);
        [Command] public record RegisterAuthor({{key}}string Name)
        {
            public {{signature}} Handle([GeneratedValue("author", Freshness = GeneratedValueFreshness.Unguarded)] AuthorId authorId{{additionalParameter}}) =>
                {{expression.Replace("WRAPPER", "new EventForEventSourceId(authorId, new AuthorRegistered(Name))", StringComparison.Ordinal).Replace("AUDIT", "new EventForEventSourceId(authorId, new AuthorAudited(Name))", StringComparison.Ordinal)}};
            {{extra}}
        }
        """;
}
#endif
