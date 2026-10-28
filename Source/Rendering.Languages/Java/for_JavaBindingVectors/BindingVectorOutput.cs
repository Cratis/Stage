// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaBindingVectors;

public sealed class BindingVectorOutput : IDisposable
{
    readonly bool _temporary;
    readonly Lazy<bool> _emission;

    public BindingVectorOutput()
    {
        var requested = Environment.GetEnvironmentVariable(when_emitting_the_binding_vectors.Variable);
        _temporary = requested is null;
        Root = requested ?? Path.Combine(Path.GetTempPath(), "stage-java-binding-vectors-" + Guid.NewGuid());
        _emission = new(() =>
        {
            JavaBindingVectorWriter.Write(Root, JavaBindingVectors.Positive(), JavaBindingVectors.Negative());
            return true;
        });
    }

    public string Root { get; }

    public void Emit() => _ = _emission.Value;

    public void Dispose()
    {
        if (_temporary && Directory.Exists(Root)) Directory.Delete(Root, recursive: true);
    }
}
