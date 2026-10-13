// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Workspaces;
using Cratis.Specifications;

namespace Cratis.Stage.Contracts.Semantics.for_StageReleaseBaseline.given;

public class a_release_baseline : Specification
{
    protected const string Source = """
        module Records
          feature Recording
            slice StateChange Record
              event Recorded
                value String
              event Removed
        """;
    protected static readonly string Key = Convert.ToHexString(Encoding.UTF8.GetBytes("source.play"));
    protected ScreenplayWorkspace _workspace = null!;
    protected ReleaseBaselineRead _baseline = null!;
    protected ReleaseBaselineAssessment _assessment = null!;

    void Establish()
    {
        var bootstrap = Workspace(Source, SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("RecordsApp")));
        var identities = bootstrap.IdentityCatalog;
        var persisted = SemanticIdentityCatalog.Create(identities.Application,
            [.. identities.Documents.Select(assignment => assignment with { Origin = SemanticIdentityOrigin.Persisted })],
            [.. identities.Semantics.Select(assignment => assignment with { Origin = SemanticIdentityOrigin.Persisted })],
            [.. identities.EventContracts.Select(assignment => assignment with { Origin = SemanticIdentityOrigin.Persisted })]);
        _workspace = Workspace(Source, persisted);
        var absent = ReleaseBaselineStore.ReadBytes(null, persisted.Application);
        var admitted = ReleaseBaseline.Admit(_workspace, absent, "v1.0", DateTimeOffset.UnixEpoch);
        _baseline = ReleaseBaselineStore.ReadBytes(ReleaseBaselineStore.Serialize(admitted.Document!), persisted.Application);
    }

    protected static ScreenplayWorkspace Workspace(string source, SemanticIdentityCatalog catalog) => ScreenplayWorkspace.Create(
        catalog.Application,
        "RecordsApp",
        [WorkspaceDocument.Create(catalog.ResolveDocument(Key), Key, PortablePlayPath.Parse("source.play"), Encoding.UTF8.GetBytes(source))],
        catalog);

    protected ScreenplayWorkspace Changed(string source) => Workspace(source, _workspace.IdentityCatalog);
}
#endif
