# Extension SDK conformance suite

`cases.json` is the language-neutral suite every Cratis Stage extension SDK passes. Each SDK builds the reference extension described in
[the conformance documentation](https://github.com/Cratis/Stage/blob/main/Documentation/extensions/conformance.md) with its own SDK, runs it on a loopback
port with a fake `StageHost`, and checks the responses and the recorded host calls against the cases.

Run it through each SDK's own tests:

| SDK | Command |
| --- | --- |
| C# | `dotnet test Source/Extensions/DotNET/Cratis.Stage.Extensions.csproj` |
| Go | `cd Source/Extensions/Go && go test ./...` |
| Kotlin | `cd Source/Extensions/Kotlin && gradle test` |
| TypeScript | `cd Source/Extensions/TypeScript && npm test` |
