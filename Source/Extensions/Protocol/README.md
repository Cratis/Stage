# Extension protocol

The `.proto` files of `cratis.stage.extensions.v1`, the two-way gRPC protocol between Cratis Stage and an extension backend.
Every SDK builds from these files and Stage itself implements them. See the
[protocol reference](https://github.com/Cratis/Stage/blob/main/Documentation/extensions/protocol.md).

```bash
buf lint
buf breaking --against '../../../.git#branch=main,subdir=Source/Extensions/Protocol'
```
