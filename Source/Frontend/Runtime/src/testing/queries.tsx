// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { registerStageQueries } from '../../../src/host/queryRegistration';
import { StageTable } from '../stageComponents';
import { useStageData } from '../stageData';

/** Complete Arc wire result for query fixtures that previously needed only data. */
export function queryResult(overrides: Record<string, unknown> = {}) {
    return {
        data: [], isReady: true, isSuccess: true, isAuthorized: true, isValid: true, hasExceptions: false,
        validationResults: [], exceptionMessages: [], exceptionStackTrace: '',
        paging: { page: 0, size: 0, totalItems: 0, totalPages: 0 },
        ...overrides,
    };
}

/** Registers the native Arc query proxies a production host registers before rendering. */
export function registerTestQueries(queries: Record<string, string>) {
    registerStageQueries({ commands: {}, queries });
}

/** Direct-table fixtures supply routes rather than compiled application bindings. */
export function RegisteredTable(props: Parameters<typeof StageTable>[0]) {
    const properties = props.element.properties;
    const data = useStageData();
    const name = properties.query ?? properties.typeName ?? props.element.id;
    const route = typeof properties.query === 'string' ? data.routes?.queries?.[properties.query] ?? properties.route : properties.route;
    if (typeof name === 'string' && typeof route === 'string' && route) registerTestQueries({ [name]: route });
    return <StageTable {...props} />;
}
