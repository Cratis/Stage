// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Stage.Contracts.Semantics.for_StageReleaseBaseline.given;

public class a_historical_release_baseline : a_release_baseline
{
    protected const string HistoricalSource = """
        concept Value : String
        module Records
          feature Recording
            slice StateChange Record
              event Recorded generation 1
                value Value
              event Recorded generation 2
                value Value
        """;

    void Establish()
    {
        _workspace = Workspace(HistoricalSource, SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("RecordsApp")));
        var absent = ReleaseBaselineStore.ReadBytes(null, _workspace.IdentityCatalog.Application);
        var admission = ReleaseBaseline.Admit(_workspace, absent, "v1.0", DateTimeOffset.UnixEpoch);
        _baseline = ReleaseBaselineStore.ReadBytes(ReleaseBaselineStore.Serialize(admission.Document!), _workspace.IdentityCatalog.Application);
    }
}
#endif
