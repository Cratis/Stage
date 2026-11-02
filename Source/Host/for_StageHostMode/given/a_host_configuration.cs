// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;

namespace Cratis.Stage.Host.for_StageHostMode.given;

public class a_host_configuration : Specification
{
    protected readonly Dictionary<string, string?> _settings = [];
    protected string[] _arguments = [];
    private protected StageRuntimeEngine _engine = StageRuntimeEngine.Semantic;
    private protected StageHostMode _mode;
    protected Exception? _error;

    private protected StageHostMode Read() => StageHostModeSelection.Read(new ConfigurationBuilder().AddInMemoryCollection(_settings).Build(), _arguments, _engine);
}
