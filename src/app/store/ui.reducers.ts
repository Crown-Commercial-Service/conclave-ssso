import { Action, ActionReducerMap, createReducer, MetaReducer, on } from '@ngrx/store';
import { environment } from 'src/environments/environment';
import { storeFreeze } from 'ngrx-store-freeze';
// import { UIActions, UIActionTypes } from './ui.actions';
import { AppStates, uiInitialState, UIState } from './ui.states';
import { SideNavClose, SideNavOpen, SideNavToggle } from './ui.actions';

const uiReducer = createReducer(
    uiInitialState,
    on(SideNavClose, (state) => ({ ...state, sideNavVisible: false })),
    on(SideNavOpen, (state) => ({ ...state, sideNavVisible: true })),
    on(SideNavToggle, (state) => ({ ...state, sideNavVisible: !state.sideNavVisible })),
);
export const metaReducers: MetaReducer<any>[] = !environment.production ? [storeFreeze] : [];
export function reducer(state: UIState | undefined, action: Action) {
    return uiReducer(state, action);
}
