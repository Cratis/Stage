// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type * as Model from '@cratis/scene.model';
import type { OutletLevel } from '@cratis/scene.engine';
import type { StageSceneApplication } from './App';

export interface NavigationFieldPolicy {
    kind: string;
    shape?: string;
    reason: string;
}
export type NavigationFields<T> = { [K in keyof T]-?: NavigationFieldPolicy };
const keep = (reason: string): NavigationFieldPolicy => ({ kind: 'keep', reason });
const child = (shape: string): NavigationFieldPolicy => ({ kind: 'node', shape, reason: `Traverse ${shape} semantically` });
const list = (shape: string): NavigationFieldPolicy => ({ kind: 'list', shape, reason: `Traverse ${shape} entries` });
const slots = { kind: 'slots', reason: 'Slot dictionary keys are slot identities; only their element lists are traversed' };
const screen = { kind: 'screen', reason: 'Application screen destination or owning screen identity' };
const identity = keep('Local identity, label or presentation value, not an application screen reference');
const data = keep('Command/query/form/binding data is a separate namespace, never inferred from its string value');
const layout = keep('Layout/template/slot/type identity is a separate namespace from application screens');
const style = keep('Layout, theme or presentation configuration contains no screen destination');
const properties = { kind: 'properties', reason: 'Use the component-specific property schema, not a global property-name or string heuristic' };

const element = {
    id: identity, properties, behaviors: list('Behavior'),
} satisfies NavigationFields<Model.SceneElement>;
const visual = {
    ...element, visibility: style, isEnabled: style, opacity: style, size: style, zIndex: style,
} satisfies NavigationFields<Model.VisualElement>;
const framework = {
    ...visual, name: identity, minimumSize: style, maximumSize: style, margin: style, horizontalAlignment: style, verticalAlignment: style,
} satisfies NavigationFields<Model.FrameworkElement>;
const control = {
    ...framework, background: style, foreground: style, borderColor: style, borderThickness: style, padding: style,
    fontFamily: style, fontSize: style, tabIndex: style,
} satisfies NavigationFields<Model.Control>;
const action = {
    kind: identity, arguments: data, onSuccess: list('Action'), onFailure: list('Action'), onResult: list('Action'),
} satisfies NavigationFields<Model.InteractionAction>;
const item = { kind: identity, id: identity, before: identity, after: identity } satisfies NavigationFields<Model.OutletItemBase>;
const outletAction = {
    ...item, title: identity, icon: identity, slice: { kind: 'slice', reason: 'Opening a slice resolves its application screen' },
    command: { kind: 'command', reason: 'Opening an outlet command resolves its slice; opens:none remains a direct command' },
    navigateTo: child('DestinationReference'), builtIn: identity, opens: identity, opensIn: layout, subject: data,
    enabledWhen: data, confirm: identity, authorize: data,
} satisfies NavigationFields<Model.OutletAction>;
const template = {
    name: layout, slots: list('Slot'), arrangement: child('Arrangement'), content: slots, displayName: identity, description: identity,
    behaviors: list('Behavior'), metadata: style, outlets: list('Outlet'),
} satisfies NavigationFields<Model.DialogTemplate>;

/** Every key of these model types is classified; a newly added key fails TypeScript until reviewed. */
export const navigationReferencePolicies = {
    Scene: {
        uiProfiles: style, themes: style, layouts: list('Layout'), screenTemplates: list('ScreenTemplate'), dialogTemplates: list('DialogTemplate'),
        screens: list('Screen'), exposures: data, instanceContributions: list('InstanceContribution'), navigation: child('NavigationModel'),
        structure: data, commandSchemas: data, outletLevels: list('OutletLevel'), responseContracts: data,
        navigationTemplates: list('ScreenTemplate'),
    } satisfies NavigationFields<StageSceneApplication & { navigationTemplates?: Model.ScreenTemplate[] }>,
    Screen: {
        name: screen, layout, slotContent: slots, forms: list('Form'), contributions: list('Contribution'),
        outletContributions: list('OutletContribution'), screenTemplate: layout, behaviors: list('Behavior'), path: data, owner: data, fits: layout,
    } satisfies NavigationFields<Model.Screen>,
    Layout: { name: layout, slots: list('Slot'), arrangement: child('Arrangement'), behaviors: list('Behavior'), metadata: style, outlets: list('Outlet') } satisfies NavigationFields<Model.Layout>,
    ScreenTemplate: { ...template, fitsSlot: layout } satisfies NavigationFields<Model.ScreenTemplate>,
    DialogTemplate: template,
    Slot: { name: layout, arrangement: child('Arrangement'), description: identity, role: layout, routed: style, routedBy: data, primary: style, optional: style, accepts: layout } satisfies NavigationFields<Model.Slot>,
    Element: { ...control, componentName: identity, slots, children: list('Element'), content: child('Element'), itemTemplate: child('Element'), itemsSource: data },
    SceneElement: element,
    VisualElement: visual,
    FrameworkElement: framework,
    Control: control,
    ExternalComponent: { ...framework, componentName: identity, slots } satisfies NavigationFields<Model.ExternalComponent>,
    Panel: { ...framework, children: list('Element') } satisfies NavigationFields<Model.Panel>,
    ContentControl: { ...control, content: child('Element') } satisfies NavigationFields<Model.ContentControl>,
    ItemsControl: { ...control, itemsSource: data, itemTemplate: child('Element') } satisfies NavigationFields<Model.ItemsControl>,
    Contribution: { contributionPointName: layout, content: { kind: 'contribution', reason: 'Navigation contributions use their consumer schema; other contribution content is an element' }, order: style } satisfies NavigationFields<Model.Contribution>,
    DestinationReference: {
        kind: identity, screen, dialog: screen, module: { kind: 'module', reason: 'Resolve the original module/feature landing screen' },
        feature: data, slice: { kind: 'destinationSlice', reason: 'Resolve the original qualified slice destination' }, slot: layout,
        route: { kind: 'route', reason: 'Internal screen route; external destinations are not application routes' }, routeParameterBindings: data,
    } satisfies NavigationFields<Model.DestinationReference>,
    NavigationModel: { application: child('ApplicationNavigation'), modules: list('ModuleNavigation') } satisfies NavigationFields<Model.NavigationModel>,
    ApplicationNavigation: { outlets: list('Outlet'), logo: style } satisfies NavigationFields<Model.ApplicationNavigation>,
    ModuleNavigation: { module: data, refinements: list('OutletRefinement'), contributions: list('OutletContribution') } satisfies NavigationFields<Model.ModuleNavigation>,
    Outlet: { name: layout, kind: identity, subject: data, slot: layout, defaults: list('OutletItem') } satisfies NavigationFields<Model.Outlet>,
    OutletRefinement: { outlet: layout, mode: identity, items: list('OutletItem'), hide: data } satisfies NavigationFields<Model.OutletRefinement>,
    OutletContribution: { outlet: layout, items: list('OutletItem') } satisfies NavigationFields<Model.OutletContribution>,
    OutletLevel: { kind: identity, name: identity, outlets: list('Outlet'), refinements: list('OutletRefinement'), contributions: list('OutletContribution') } satisfies NavigationFields<OutletLevel>,
    OutletItemBase: item,
    OutletAction: outletAction,
    OutletButton: { ...item, ...outletAction, emphasis: style, display: style } satisfies NavigationFields<Model.OutletButton>,
    OutletMenuEntry: { ...item, ...outletAction } satisfies NavigationFields<Model.OutletMenuEntry>,
    OutletNavigationItem: { ...item, title: identity, icon: identity, target: child('DestinationReference'), slot: layout, children: list('OutletItem') } satisfies NavigationFields<Model.OutletNavigationItem>,
    OutletGroup: { ...item, title: identity, icon: identity, items: list('OutletItem') } satisfies NavigationFields<Model.OutletGroup>,
    OutletTab: { ...item, title: identity, shows: screen } satisfies NavigationFields<Model.OutletTab>,
    OutletFilter: { ...item, title: identity, control: style, sets: data } satisfies NavigationFields<Model.OutletFilter>,
    OutletStat: { ...item, title: identity, valueFrom: data, trend: data } satisfies NavigationFields<Model.OutletStat>,
    OutletSeparator: item satisfies NavigationFields<Model.OutletSeparator>,
    Behavior: { name: identity, bindings: list('InteractionBinding'), order: style } satisfies NavigationFields<Model.Behavior>,
    InteractionBinding: { trigger: data, actions: list('Action'), condition: data, alternatives: list('InteractionAlternative'), otherwise: list('Action') } satisfies NavigationFields<Model.InteractionBinding>,
    InteractionAlternative: { condition: data, actions: list('Action') } satisfies NavigationFields<Model.InteractionAlternative>,
    InteractionAction: action,
    NavigateAction: { ...action, screen } satisfies NavigationFields<Model.NavigateAction>,
    OpenDialogAction: { ...action, dialogTemplate: screen } satisfies NavigationFields<Model.OpenDialogAction>,
    ExecuteCommandAction: { ...action, command: data } satisfies NavigationFields<Model.ExecuteCommandAction>,
    SetStateAction: { ...action, target: data, value: data } satisfies NavigationFields<Model.SetStateAction>,
    RefreshQueryAction: { ...action, query: data } satisfies NavigationFields<Model.RefreshQueryAction>,
    NotifyAction: { ...action, level: identity, message: data } satisfies NavigationFields<Model.NotifyAction>,
    ConfirmAction: { ...action, message: data } satisfies NavigationFields<Model.ConfirmAction>,
    RaiseTriggerAction: { ...action, trigger: data } satisfies NavigationFields<Model.RaiseTriggerAction>,
    NavigateBackAction: action satisfies NavigationFields<Model.NavigateBackAction>,
    CloseDialogAction: action satisfies NavigationFields<Model.CloseDialogAction>,
    Form: { name: identity, forCommand: data, populateSource: data, generationMode: style, layout: style, fields: data, behaviors: list('Behavior'), onSubmit: list('Action') } satisfies NavigationFields<Model.Form>,
    InstanceContribution: {
        instance: { kind: 'instanceOwner', reason: 'Only screen:/dialog: owner protocols name application destinations' }, component: identity, path: data,
        value: { kind: 'instanceValue', reason: 'The exposed property path determines its semantic policy; arbitrary exposed data is preserved' },
        items: { kind: 'instanceItems', reason: 'Apply the exposed collection consumer schema to contributed values' }, replaceOwnerItems: style,
    } satisfies NavigationFields<Model.InstanceContribution>,
    ContributedItem: { id: identity, values: data } satisfies NavigationFields<Model.ContributedItem>,
    GuardedActionProperties: { label: identity, alternatives: data, item: data, otherwise: data, navigateToScreen: screen, navigateByParameter: data } satisfies NavigationFields<Model.GuardedActionProperties>,
    Toolbar: { buttons: list('ToolbarButton') } satisfies NavigationFields<Model.Toolbar>,
    ToolbarButton: { id: identity, title: identity, slice: { kind: 'slice', reason: 'Legacy toolbar slice opening' }, emphasis: style, icon: identity, target: { kind: 'legacyCommand', reason: 'Legacy toolbar command resolves its opening slice' } } satisfies NavigationFields<Model.ToolbarButton>,
};

/** Explicit property paths read by Scene/blueprint adapters. Open JSON data is not recursively interpreted as navigation. */
export const navigationPropertyPolicies: Record<string, Record<string, NavigationFieldPolicy>> = {
    authoredNavigation: { targetScreen: screen, destination: child('DestinationReference'), navigateTo: child('DestinationReference'), homeTargetScreen: screen },
    breadcrumb: { homeTargetScreen: screen, items: list('BreadcrumbItem') },
    menu: { items: list('MenuEntry') },
    menuItem: { targetScreen: screen, destination: child('DestinationReference'), items: list('MenuEntry'), children: list('MenuEntry') },
    logo: { targetScreen: screen, destination: child('DestinationReference') },
    userMenu: { actions: list('MenuEntry') },
    button: { targetScreen: screen, destination: child('DestinationReference') },
    'core:button': { targetScreen: screen, destination: child('DestinationReference') },
    'core:guarded-action': navigationReferencePolicies.GuardedActionProperties,
    'core:action': { navigateToScreen: screen },
    'Stage:commandForm': { navigateToScreen: screen },
    'Stage:extensionComponent': { screen },
    'core:table': { navigateOnRowClickToScreen: screen },
    'core:slot': { shows: screen },
    'core:outletItems': { items: list('OutletItem') },
    'core:navigationBar': { items: list('MenuEntry') },
    'core:toolbar': { buttons: list('LegacyToolbarButton') },
    'core:toolbarItem': { ...navigationReferencePolicies.ToolbarButton, navigateTo: child('DestinationReference'), destination: child('DestinationReference') },
    'core:extension': { screen },
};

/** Open blueprint item payloads: ids/labels/routes are classified by their consumer, not by matching any string. */
export const navigationItemPolicies = {
    BreadcrumbItem: { targetScreen: screen, screen, destination: child('DestinationReference') },
    MenuEntry: { targetScreen: screen, destination: child('DestinationReference'), target: child('DestinationReference'), children: list('MenuEntry'), items: list('MenuEntry') },
};

const flow = { grow: style, span: style } satisfies NavigationFields<Model.FlowNode>;
const flowContainer = { ...flow, children: list('FlowNode'), gap: style, kind: style } satisfies NavigationFields<Model.FlowContainer>;
const placement = { element: child('Element'), x: style, y: style, width: style, height: style } satisfies NavigationFields<Model.ElementPlacement>;
export const arrangementNavigationPolicies = {
    SceneDocument: {
        layouts: list('Layout'), screenTemplates: list('ScreenTemplate'), dialogTemplates: list('DialogTemplate'), screens: list('Screen'),
        exposures: data, instanceContributions: list('InstanceContribution'),
    } satisfies NavigationFields<Model.SceneDocument>,
    Arrangement: { root: child('FlowNode'), overrides: list('FlowOverride'), variants: list('FreeformVariant') },
    FlowArrangement: { root: child('FlowNode'), overrides: list('FlowOverride') } satisfies NavigationFields<Model.FlowArrangement>,
    FlowNode: { ...flowContainer, content: child('Element'), slotName: layout, columns: style, rows: style },
    FlowContainer: flowContainer,
    FlowRow: flowContainer satisfies NavigationFields<Model.FlowRow>,
    FlowColumn: flowContainer satisfies NavigationFields<Model.FlowColumn>,
    FlowGrid: { ...flowContainer, columns: style, rows: style } satisfies NavigationFields<Model.FlowGrid>,
    FlowLeaf: { ...flow, content: child('Element') } satisfies NavigationFields<Model.FlowLeaf>,
    FlowSlotLeaf: { ...flow, slotName: layout } satisfies NavigationFields<Model.FlowSlotLeaf>,
    FlowOverride: { width: style, height: style, root: child('FlowNode') } satisfies NavigationFields<Model.FlowOverride>,
    FreeformArrangement: { variants: list('FreeformVariant') } satisfies NavigationFields<Model.FreeformArrangement>,
    FreeformSlotArrangement: { variants: list('FreeformSlotVariant') } satisfies NavigationFields<Model.FreeformSlotArrangement>,
    FreeformVariant: { sizeClass: style, placements: list('ElementPlacement') } satisfies NavigationFields<Model.FreeformVariant>,
    FreeformSlotVariant: { sizeClass: style, placements: list('SlotPlacement') } satisfies NavigationFields<Model.FreeformSlotVariant>,
    ElementPlacement: placement,
    SlotPlacement: { slotName: layout, x: style, y: style, width: style, height: style } satisfies NavigationFields<Model.SlotPlacement>,
};
