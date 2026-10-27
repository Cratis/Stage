<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Stage templates

The prerelease `stage-templates/v1` profile renders JSON through declared, in-memory kit templates. Scriban is exactly pinned to 7.5.3; its types are implementation details, not the public contract. No production language pack is included.

All STAGE-TPL codes are safety refusals: trust, integrity or budget failures. A refused plan has no usable outputs, including artifacts returned before refusal. The caller must discard them all.

Only allowlisted standard-library members and declared helpers are available; there is no clock, randomness, environment access, dynamic evaluation or JSON parsing. Scope values are JSON, never arbitrary host objects.

## Budgets

Default aggregate budgets are 2,000,000 steps, 8 MiB UTF-8 output, 2,000 artifacts, 2,000 fan-out items, 256 KiB string length, recursion depth 64 and a 10-second deadline. Preparation limits each template to 64 KiB UTF-8, semantic block depth 32 and unary/delimiter expression nesting 32. An `else`/`else if` chain stays at its `if` block's semantic level. The caller owns the process-level CPU and memory sandbox.

Scriban's parser depth backstop remains 64. Else-if chains count toward parser depth even though they share one semantic block level, so chains around 60 branches can reach the parser limit, depending on surrounding expressions and blocks. Parser depth, insufficient-stack and semantic nesting failures are refused with `STAGE-TPL-003`.

## Compatibility

Helper bytes, signatures and semantics are frozen once released. Changing them requires a new helper name or `templates/v2`. Frozen vectors only grow; the surface digest pins the versioned contract. `HelperCatalogue.GenerateReference()` generates the checked-in [helper reference](../../Documentation/reference/template-helpers.md) from those vectors, codes, allowlist and defaults.
