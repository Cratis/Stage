// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;

namespace Cratis.Stage.Rendering.Declarative.for_DeclarativeArtifactRenderPlanner.given;

/// <summary>
/// Plans the test kit with implementation requirements and bodies supplied on the request.
/// </summary>
public class a_body_planner : a_declarative_planner
{
    internal static string Hash(string body) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(body)));

    internal static SemanticImplementationRequirement Requirement(string id, string body, string? language = "test", string? hash = null) =>
        new(SemanticImplementationRole.CommandHandler, SemanticAddress.ForApplication(ApplicationIdentity.Create("Sample")), "Handle", language, "handler.test", hash ?? Hash(body), null!) { RequirementId = id };

    internal static ArtifactRenderPlan PlanBody(Dictionary<string, string> templates, SemanticImplementationRequirement? requirement, string? body)
    {
        var files = KitFiles();
        foreach (var (path, text) in templates)
        {
            files[path] = Bytes(text);
        }

        var request = Request(Profile(Archive(files))) with
        {
            ImplementationRequirements = requirement is null ? [] : [requirement],
            ImplementationContents = requirement is null || body is null ? [] : ImmutableDictionary<string, string>.Empty.Add(requirement.RequirementId, body)
        };
        return Planner().Plan(request);
    }
}
