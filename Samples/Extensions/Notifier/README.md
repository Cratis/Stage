# Notifier

A minimal Stage extension with a backend ability and a React component. Copy it to start your own, or run it as it is in the specifications of
[From a model to a running extension](https://github.com/Cratis/Stage/blob/main/Documentation/extensions/end-to-end.md).

| Path | What it is |
| --- | --- |
| `contract.play` | The contract: the `Notify` ability, the `NoticeBoard` component, the `channel` and `token` settings, the backend and the frontend entry. |
| `frontend/index.js` | The `NoticeBoard` component, built on `@cratis/scene.extensions`. |
| `placeholders/notice-board.html` | The static placeholder design tools draw in place of the component. |
| `assets/icon.svg` | The icon of the package. |
| `backend/` | The backend, a C# project on the [extension SDK](https://github.com/Cratis/Stage/blob/main/Documentation/extensions/csharp.md). |

## Use it from a model

```screenplay
system Notifier
  extension "ghcr.io/acme/extension-notifier"

screen NoticeBoard
  component Notifier.NoticeBoard board
    property title = "Today"
```

Pack the folder, sign it and publish it as an OCI artifact, then `screenplay restore` pins it in `screenplay.lock`. Stage loads the package from
the lock, starts the backend when someone reaches the screen and answers `Notify` through the durable invocation path.
