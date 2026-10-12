import { createElement } from 'react';

// A Scene runtime package: its declaration (the manifest) and the components it registers, keyed `<package>:<component>`.
function Chart({ element }) {
    const title = typeof element.properties?.title === 'string' ? element.properties.title : 'Chart';
    return createElement('figure', { className: 'acme-chart', 'data-acme-chart': element.id }, createElement('figcaption', null, title));
}

export const acmeChartsRuntimePackage = {
    manifest: {
        name: 'Acme.Charts',
        version: '1.2.0',
        kind: 'ComponentLibrary',
        dependencies: [],
        components: ['chart'],
        layouts: [],
        screenTemplates: [],
        dialogTemplates: [],
        themes: [],
        module: '@acme/scene.charts',
    },
    components: { 'Acme.Charts:chart': Chart },
};
