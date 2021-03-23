import { select, Store } from "@ngrx/store";
import { Observable } from "rxjs";

import { getSideNavVisible } from "src/app/store/ui.selectors";
import { UIState } from "src/app/store/ui.states";

export class BaseComponent {
    
    public sideNavVisible$: Observable<boolean>;

    constructor(protected uiStore: Store<UIState>) {
        this.sideNavVisible$ = Observable.of(false);
        this.sideNavVisible$ = this.uiStore.pipe(select(getSideNavVisible));
    }
}
