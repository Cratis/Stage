// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Specifications;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticUnknownMembers.when_finding.given;

public class a_populated_model : Specification
{
    protected ExecutableSemanticModel _model = null!;
    private protected Dictionary<string, SemanticSurfaceDisposition> _known = null!;
    protected SemanticCommand[] _readingCommands = null!;
    protected SemanticCommand _readingCommand = null!;
    private protected ImmutableArray<SemanticUnknownMember> _unknown;

    void Establish()
    {
        _model = SemanticModelSerializer.Deserialize(ProtectedCommandReadsCorpus.V25.EsmBytes.AsSpan());
        _known = new(SemanticSurfaceLedger.Entries, StringComparer.Ordinal);
        _readingCommands = [.. _model.Application.Modules.SelectMany(module => module.Features)
            .SelectMany(feature => feature.Slices).SelectMany(slice => slice.Commands).Where(command => command.Reads.Length > 0)];
        _readingCommand = _readingCommands[0];
    }

    protected void AddAuthenticatedPolicy() => _model = ExecutableSemanticModel.Create(
        _model.LanguageVersion,
        _model.SemanticVersion,
        _model.Application with { Policies = [new("Sensitive policy name", new SemanticAuthenticatedCondition())] });
}
