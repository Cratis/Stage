// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Semantics;

namespace Cratis.Stage.Host;

internal sealed record HostAdmissionEntry(string Artifact, string Kind, string Status, string? Capability, string? Details);

// Lets the conformance matrix run the shipped admission without seeing the Stage library's internals.
internal static class HostAdmission
{
    internal static IReadOnlyList<HostAdmissionEntry> Entries(SemanticExecutionPlan plan) =>
        [.. new SemanticRuntimeAdmission(plan).Entries.Select(entry => new HostAdmissionEntry(entry.Artifact, entry.Kind, entry.Status, entry.Capability, entry.Details))];
}
