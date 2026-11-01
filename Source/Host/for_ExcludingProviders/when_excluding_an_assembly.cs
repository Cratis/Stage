// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Queries;
using Cratis.Specifications;
using Cratis.Types;
using NSubstitute;
using Xunit;

namespace Cratis.Stage.Host.Published.for_ExcludingProviders;

public class when_excluding_an_assembly : Specification
{
    sealed record Kept;

    ICommandHandlerProviders _commands = null!;
    IQueryPerformerProviders _queries = null!;
    ICommandHandler _excludedHandler = null!;
    ICommandHandler _keptHandler = null!;

    void Establish()
    {
        _excludedHandler = Handler(typeof(string));
        _keptHandler = Handler(typeof(Kept));

        var commandProvider = Substitute.For<ICommandHandlerProvider>();
        commandProvider.Handlers.Returns([_excludedHandler, _keptHandler]);
        var queryProvider = Substitute.For<IQueryPerformerProvider>();
        var excludedPerformer = Performer("Excluded", typeof(string));
        var keptPerformer = Performer("Kept", typeof(Kept));
        queryProvider.Performers.Returns([excludedPerformer, keptPerformer]);

        var services = new ServiceCollection();
        services.AddSingleton(Instances(commandProvider));
        services.AddSingleton(Instances(queryProvider));
        services.ExcludeFromArc([typeof(string).Assembly]);
        var provider = services.BuildServiceProvider();
        _commands = provider.GetRequiredService<ICommandHandlerProviders>();
        _queries = provider.GetRequiredService<IQueryPerformerProviders>();
    }

    static ICommandHandler Handler(Type commandType)
    {
        var handler = Substitute.For<ICommandHandler>();
        handler.CommandType.Returns(commandType);
        return handler;
    }

    static IQueryPerformer Performer(string name, Type type)
    {
        var performer = Substitute.For<IQueryPerformer>();
        performer.Type.Returns(type);
        performer.ReadModelType.Returns(type);
        performer.Name.Returns(new QueryName(name));
        performer.Parameters.Returns(new QueryParameters([]));
        performer.FullyQualifiedName.Returns(new FullyQualifiedQueryName($"x.{name}"));
        return performer;
    }

    static IInstancesOf<T> Instances<T>(T instance)
        where T : class
    {
        var instances = Substitute.For<IInstancesOf<T>>();
        instances.GetEnumerator().Returns(_ => new List<T> { instance }.GetEnumerator());
        return instances;
    }

    [Fact] void should_leave_the_excluded_commands_out() => _commands.Handlers.All(handler => handler.CommandType != typeof(string)).ShouldBeTrue();
    [Fact] void should_keep_the_other_commands() => _commands.Handlers.ShouldContain(_keptHandler);
    [Fact] void should_not_resolve_an_excluded_command() => _commands.TryGetHandlerFor("x", out _).ShouldBeFalse();
    [Fact] void should_leave_the_excluded_queries_out() => _queries.Performers.All(performer => performer.Type != typeof(string)).ShouldBeTrue();
    [Fact] void should_keep_the_other_queries() => _queries.Performers.Count().ShouldEqual(1);
    [Fact] void should_not_resolve_an_excluded_query() => _queries.TryGetPerformersFor(new FullyQualifiedQueryName("x.Excluded"), out _).ShouldBeFalse();
    [Fact] void should_resolve_a_query_that_is_kept() => _queries.TryGetPerformersFor(new FullyQualifiedQueryName("x.Kept"), out _).ShouldBeTrue();
}
