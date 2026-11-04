// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Stage.Rendering.Cratis.CodeGeneration;

namespace Cratis.Stage.Rendering.Cratis.for_ArcScreenplayRoundTrip.given;

internal static class native_policy_sources
{
    internal static RenderedFile[] All() =>
    [
        new("Banking/Program.cs", "namespace Banking;"),
        new("Banking/Composition.cs", """
            using Cratis.Arc.Authorization;
            using Microsoft.Extensions.DependencyInjection;
            using Banking.Accounting.Booking;
            using Banking.Automation.Jobs;
            namespace Banking;
            internal static class Registration
            {
                internal static void Add(string name, System.Action<IServiceCollection> apply) { }
            }
            internal static class Policies
            {
                [System.Runtime.CompilerServices.ModuleInitializer]
                internal static void Init()
                {
                    Registration.Add("Accountant", services => services.AddArcAuthorizationPolicy<AccountantPolicy>("Accountant"));
                    Registration.Add("Automation", services => services.AddArcAuthorizationPolicy<AutomationPolicy>("Automation"));
                }
            }
            """),
        new("Banking/Accounting/Booking/BookEntry.cs", Command("BookEntry", "Accounting/Booking", "Accountant")),
        new("Banking/Automation/Jobs/RunJob.cs", Command("RunJob", "Automation/Jobs", "Automation")),
        new("Banking/Accounting/Booking/AccountantPolicy.cs", Policy("AccountantPolicy", "Accounting.Booking", "context.Principal.Identity?.IsAuthenticated == true && context.Principal.IsInRole(\"Accountant\")")),
        new("Banking/Automation/Jobs/AutomationPolicy.cs", Policy("AutomationPolicy", "Automation.Jobs", "context.Principal.IsInRole(\"Automation\")"))
    ];

    static string Command(string name, string path, string policy) => $$"""
        using Cratis.Arc.Authorization;
        using Cratis.Arc.Commands.ModelBound;
        using Cratis.Chronicle.Events;
        namespace Banking.{{path.Replace('/', '.')}};
        [Command]
        [Authorize(Policy = "{{policy}}")]
        public record {{name}}(string Name)
        {
            public {{name}}Done Handle() => new(Name);
        }
        [EventType] public record {{name}}Done(string Name);
        """;

    static string Policy(string name, string path, string expression) => $$"""
        using Cratis.Arc.Authorization;
        using System.Threading;
        using System.Threading.Tasks;
        namespace Banking.{{path}};
        public sealed class {{name}} : IAuthorizationPolicy
        {
            public ValueTask<bool> IsAuthorized(AuthorizationPolicyContext context, CancellationToken cancellationToken)
                => ValueTask.FromResult({{expression}});
        }
        """;
}
#endif
