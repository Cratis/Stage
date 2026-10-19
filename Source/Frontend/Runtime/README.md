# @cratis/stage.runtime

The Cratis Stage runtime renders Scene documents using Arc, Cratis Components and the default Scene blueprint.
It shares React, Arc contexts and Scene's command registry with the consuming application through exact peer dependencies.

Import `App` (also exported as `StageApplication`), `StageSourceProvider`, `staticStageSource` and the corresponding types from `@cratis/stage.runtime`.
Wrap the application in `StageSourceProvider` with its content source. The live Stage host supplies HTTP transport;
a built application can supply `staticStageSource({ scene, routes, strings })` instead.

Use `styledPrimeReact` from `@cratis/stage.runtime/vite` in the consuming Vite configuration.
The runtime imports its own CSS; `@cratis/stage.runtime/styles.css` also exposes it for host-only surfaces.

The runtime's version follows the Stage release. Generated applications continue to receive the embedded runtime until the renderer switches to this package.
