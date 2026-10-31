// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reflection;
using Cratis.Arc.Authorization;
using Cratis.Arc.Queries;
using Cratis.Arc.Tenancy;
using Cratis.Chronicle.ReadModels;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;
using Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticImplementationAdmission.when_admitting_performers.given;
using NSubstitute;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticStateViewArtifactRenderer.when_streaming_performed_queries.given;

public class a_live_query : Specification
{
    private protected Probe _stream = null!;
    protected object[] _dependencies = null!;
    protected MethodInfo _method = null!;

    protected async Task Prepare(string body = "return readModels;")
    {
        var loaded = await when_rendering_a_pure_reducer.Load(a_performer.Source(body));
        _method = when_executing_performed_queries.Method(loaded);
        _stream = (Probe)Activator.CreateInstance(typeof(LiveProbe<>).MakeGenericType(_method.DeclaringType));
        _dependencies = when_executing_performed_queries.Dependencies(_stream.Store);
        _stream.Attach(_method, _dependencies, when_executing_performed_queries.Rows(_method).Rows);
    }

    protected void ChangeInvocationContext()
    {
        ((ITenantIdAccessor)_dependencies[2]).Current.Returns(new TenantId("tenant-b"));
        ((IQueryContextManager)_dependencies[1]).Current.Returns(QueryContext.NotSet with { ReceivedAt = DateTimeOffset.MaxValue });
        ((ICurrentPrincipalAccessor)_dependencies[3]).Current.Returns(new System.Security.Claims.ClaimsPrincipal());
    }

    private protected abstract class Probe
    {
        internal IReadModels Store { get; } = Substitute.For<ObservableReadModels>();
        internal List<Array> Emissions { get; } = [];
        internal List<Exception> Errors { get; } = [];
        internal int Subscriptions { get; set; }
        internal int Disposals { get; set; }
        internal bool Completed { get; set; }
        internal abstract void Attach(MethodInfo method, object[] dependencies, Array rows);
        internal abstract void Invoke();
        internal abstract void Subscribe();
        internal abstract void SendRows();
        internal abstract void Complete();
        internal abstract void Fail(Exception exception);
        internal abstract void FailRelay(Exception exception);
        internal abstract void Disconnect();
    }

    sealed class LiveProbe<T> : Probe
        where T : class
    {
        readonly ReplaySubject<IEnumerable<T>> _source = new(1);
        MethodInfo _query = null!;
        object[] _arguments = null!;
        IEnumerable<T> _rows = null!;
        ISubject<IEnumerable<T>> _result = null!;
        IDisposable _subscription = null!;

        internal override void Attach(MethodInfo method, object[] dependencies, Array rows)
        {
            _query = method;
            _arguments = [.. dependencies, "Ada"];
            _rows = rows.Cast<T>().Append((T)Activator.CreateInstance(typeof(T), Guid.NewGuid(), "Grace")!);
            var observable = Observable.Create<IEnumerable<T>>(observer =>
            {
                Subscriptions++;
                return new CompositeDisposable(_source.Subscribe(observer), Disposable.Create(() => Disposals++));
            });
            Store.Observe<T>().Returns(Subject.Create<IEnumerable<T>>(_source, observable));
        }

        internal override void Invoke() => _result = (ISubject<IEnumerable<T>>)_query.Invoke(null, _arguments);
        internal override void Subscribe() => _subscription = _result.Subscribe(value => Emissions.Add(value.ToArray()), Errors.Add, () => Completed = true);
        internal override void SendRows() => _source.OnNext(_rows);
        internal override void Complete() => _source.OnCompleted();
        internal override void Fail(Exception exception) => _source.OnError(exception);
        internal override void FailRelay(Exception exception) => _result.OnError(exception);
        internal override void Disconnect() => _subscription.Dispose();
    }
}
#endif
