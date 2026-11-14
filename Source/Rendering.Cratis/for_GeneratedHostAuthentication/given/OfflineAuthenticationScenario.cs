// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Cratis.for_GeneratedHostAuthentication.given;

sealed class OfflineAuthenticationScenario(OfflineAuthenticationFixture owner, OfflineAuthenticationScenarioState state) : IDisposable
{
    bool _disposed;

    internal OfflineAuthenticationDiagnostics Diagnostics => state.Diagnostics;

    public void Dispose()
    {
        if (!_disposed)
        {
            owner.EndScenario(state);
            _disposed = true;
        }
    }
}
