// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Reflection;
using Cratis.Arc.Authorization;
using Cratis.Arc.Commands;
using Cratis.Chronicle.ReadModels;
using Cratis.Execution;
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.Policies.for_StatePolicies;

// A policy that reads recorded state renders as an Arc policy that denies whenever the state cannot be had: absent, enabled by nobody, or unreadable.
public class when_rendering_a_policy_that_decides_from_recorded_state : Specification
{
    const string Source = """
        concept InvoiceId : String
        policy PlanAllows
          reads InvoicePlan as plan by invoiceId
          require authenticated and plan.enabled == true
        module Billing
          feature Invoicing
            slice StateChange Issue
              command IssueInvoice
                authorize PlanAllows
                invoiceId InvoiceId identifier
                description String
                produces InvoiceIssued
                  for invoiceId
                  invoiceId = invoiceId
                  description = description
              event InvoiceIssued
                invoiceId InvoiceId
                description String
            slice StateView Plans
              readmodel InvoicePlan
                invoiceId InvoiceId key
                enabled Bool
              event PlanSet
                enabled Bool
              projection BuildPlan => InvoicePlan
                from PlanSet
                  invoiceId = $eventSourceId
                  enabled = enabled
        """;

    ArtifactRenderPlan _plan = null!;
    Assembly _assembly = null!;
    ExecutableSemanticModel _model = null!;
    bool _enabled;
    bool _disabled;
    bool _absent;
    bool _unreadable;
    bool _guest;

    void Because()
    {
        var loaded = opaque_policy_model.Load(Source);
        _model = loaded.Model;
        _plan = opaque_policy_model.Plan(loaded);
        Assert.True(_plan.Success, opaque_policy_model.Errors(_plan));
        _assembly = opaque_policy_model.Compile(_plan);
        _enabled = Allows(planEnabled: true, unreadable: false, authenticated: true);
        _disabled = Allows(planEnabled: false, unreadable: false, authenticated: true);
        _absent = Allows(planEnabled: null, unreadable: false, authenticated: true);
        _unreadable = Allows(planEnabled: true, unreadable: true, authenticated: true);
        _guest = Allows(planEnabled: true, unreadable: false, authenticated: false);
    }

    [Fact] void should_render_the_policy() => _plan.Success.ShouldBeTrue();
    [Fact] void should_allow_when_the_recorded_plan_is_enabled() => _enabled.ShouldBeTrue();
    [Fact] void should_deny_when_the_recorded_plan_is_disabled() => _disabled.ShouldBeFalse();
    [Fact] void should_deny_when_no_plan_is_recorded() => _absent.ShouldBeFalse();
    [Fact] void should_deny_when_the_plan_cannot_be_read() => _unreadable.ShouldBeFalse();
    [Fact] void should_deny_a_caller_who_is_not_authenticated() => _guest.ShouldBeFalse();

    bool Allows(bool? planEnabled, bool unreadable, bool authenticated)
    {
        var command = _model.Application.Modules.Single().Features.Single().Slices.SelectMany(slice => slice.Commands).Single();
        var type = _assembly.GetTypes().Single(candidate => candidate.Name == "IssueInvoice");
        var invoiceId = _assembly.GetTypes().Single(candidate => candidate.Name == "InvoiceId");
        var plan = _assembly.GetTypes().Single(candidate => candidate.Name == "InvoicePlan");
        var instance = Activator.CreateInstance(type, Activator.CreateInstance(invoiceId, "invoice-one"), "North");
        var stored = planEnabled is null ? null : CreatePlan(plan, invoiceId, planEnabled.Value);
        var readModels = ReadModelsProxy.Create(plan, stored, unreadable);
        var policyType = _assembly.GetTypes().Single(candidate => candidate.Name == policy_names.ClassOf(_model, command.Id));
        var policy = (IAuthorizationPolicy)Activator.CreateInstance(policyType, readModels)!;
        var principal = authenticated ? opaque_policy_model.Caller() : opaque_policy_model.Guest();
        var context = new AuthorizationPolicyContext(principal, type, new CommandContext(CorrelationId.New(), type, instance, [], CommandContextValues.Empty)) { ReceivedAt = opaque_policy_model.Receipt };

        return policy.IsAuthorized(context, CancellationToken.None).AsTask().GetAwaiter().GetResult();
    }

    static object CreatePlan(Type plan, Type invoiceId, bool enabled)
    {
        var constructor = plan.GetConstructors().OrderByDescending(candidate => candidate.GetParameters().Length).First();
        var arguments = constructor.GetParameters().Select(Argument).ToArray();

        return constructor.Invoke(arguments);

        object? Argument(ParameterInfo parameter)
        {
            if (parameter.ParameterType == typeof(bool))
            {
                return enabled;
            }

            return parameter.ParameterType == invoiceId ? Activator.CreateInstance(invoiceId, "invoice-one") : null;
        }
    }

    public class ReadModelsProxy : DispatchProxy
    {
        Type _readModelType = null!;
        object? _stored;
        bool _unreadable;

        public static IReadModels Create(Type readModelType, object? stored, bool unreadable)
        {
            var proxy = Create<IReadModels, ReadModelsProxy>();
            var self = (ReadModelsProxy)(object)proxy;
            self._readModelType = readModelType;
            self._stored = stored;
            self._unreadable = unreadable;

            return proxy;
        }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod!.Name != nameof(IReadModels.GetInstanceById) || !targetMethod.IsGenericMethod)
            {
                throw new NotSupportedException(targetMethod.Name);
            }

            var result = targetMethod.GetGenericArguments()[0];
            if (_unreadable)
            {
                return typeof(ReadModelsProxy).GetMethod(nameof(Failed), BindingFlags.NonPublic | BindingFlags.Static)!.MakeGenericMethod(result).Invoke(null, null);
            }

            return typeof(Task).GetMethod(nameof(Task.FromResult))!.MakeGenericMethod(result).Invoke(null, [_stored]);
        }

        static Task<T> Failed<T>() => Task.FromException<T>(new InvalidOperationException("The read model store is unavailable."));
    }
}
#endif
