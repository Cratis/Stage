// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Screenplay;
using Xunit;

namespace Cratis.Stage.Contracts.for_EventModelRuleAdmission.given;

public class a_modeled_rule : Specification
{
    protected const string Source = """
        concept RecordId : Uuid
        concept Name : String
        module Registry
          feature Records
            slice StateChange Register
              command RegisterRecord
                recordId RecordId identifier
                name Name
                validate
                  name not empty
                produces RecordRegistered
                  recordId = recordId
                  name = name
              event RecordRegistered
                recordId RecordId
                name Name
            slice StateView List
              readmodel Records
                recordId RecordId key
                name Name
              query ListRecords => Records[]
        """;

    protected string _source = Source;
    protected EventModel _model = null!;
    protected Exception? _refusal;

    protected InvalidEventModel Refusal => Assert.IsType<InvalidEventModel>(_refusal);

    protected void Admit()
    {
        _model = EventModelLoader.LoadFromSource(_source);
        _refusal = Catch.Exception(() => EventModelRuleAdmission.EnsureExecutable(_model, "registry.play"));
    }
}
