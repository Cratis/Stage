// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using System.Text.Json;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Semantics;
using NSubstitute;

namespace Cratis.Stage.Host.for_SemanticRuntime.given;

public class a_generated_runtime : Specification
{
    protected ISemanticRuntime _runtime = null!;
    protected SemanticCommand _command = null!;
    protected object _appender = null!;

    void Establish()
    {
        var plan = compiled_plan.From("""
            concept Id : Uuid
            policy SignedIn
              require authenticated
            module M
              feature F
                slice StateChange S
                  command C
                    id Id generated identifier
                    name String
                    authorize SignedIn
                    validate
                      name not empty
                    produces event Created
                      name String = name
                    returns id
            """);
        var appender = Substitute.For<IAppendSemanticFacts, ISemanticFactTail>();
        ((ISemanticFactTail)appender).Tail().Returns(ulong.MaxValue);
        _appender = appender;
        _runtime = SemanticRuntimeHosting.Create(plan, appender);
        _command = plan.Commands.Values.Single();
    }

    protected Task<SemanticExecutionResult> Execute(string name = "Apollo", bool validateOnly = true, bool authenticated = true, bool forged = false)
    {
        var payload = new Dictionary<string, JsonElement> { ["name"] = JsonSerializer.SerializeToElement(name) };
        if (forged) payload["id"] = JsonSerializer.SerializeToElement("11111111-1111-1111-1111-111111111111");
        return _runtime.Execute(_command, payload, new ClaimsPrincipal(new ClaimsIdentity([], authenticated ? "spec" : null)), new(DateTimeOffset.UtcNow, "subject", "name", "user"), validateOnly);
    }

    void Destroy() => ((IDisposable)_runtime).Dispose();
}
