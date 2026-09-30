// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import Lara from '@primeuix/themes/lara';

/**
 * The class that puts a Stage application in its dark scheme. Stage is always dark (see app.css), so it is
 * set on the document root before anything renders rather than following the operating system.
 */
export const stageDarkClass = 'stage-dark';

/**
 * The PrimeReact theme a Stage application renders with.
 *
 * PrimeReact 11 ships no CSS of its own: a component is styled only when the provider above it is handed a
 * theme preset, from which it injects its styles at runtime. Without one, every component still renders its
 * markup - a dialog included - but with nothing positioning or painting it, so a command's dialog opened as
 * unstyled content below the screen and a user saw nothing happen when they clicked the command's button.
 */
export const stageTheme = {
    preset: Lara,
    options: {
        darkModeSelector: `.${stageDarkClass}`,
    },
};
