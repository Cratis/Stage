// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;

namespace Cratis.Stage.Host.for_SemanticHost.given;

public class a_model_path : Specification
{
    protected string _directory = null!;
    protected string _path = null!;

    void Establish()
    {
        _directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_directory);
        _path = Path.Combine(_directory, "model.play");
    }

    void Destroy() => Directory.Delete(_directory, true);
}
