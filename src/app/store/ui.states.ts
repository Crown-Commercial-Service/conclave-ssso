import { createFeatureSelector } from "@ngrx/store";

export interface UIState {
    sideNavVisible: boolean;
}
export const uiInitialState: UIState = {
    sideNavVisible: false,
}
export interface AppStates {
    uiState: UIState;
}
