import { createFeatureSelector, createSelector } from '@ngrx/store';
import { AppStates, UIState } from './ui.states';

export const selectFeature = createFeatureSelector<UIState>('ui-state');
export const getSideNavVisible = createSelector(
  selectFeature,
  (state: UIState) => state.sideNavVisible
);
