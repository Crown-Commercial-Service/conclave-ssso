import { Injectable } from '@angular/core';

@Injectable({
    providedIn: 'root'
})
export class ScrollHelper {
    private elementIdToScrollTo: string | null = null;

    scrollToFirst(elementId: string) {
        this.elementIdToScrollTo = elementId;
    }

    doScroll() {
        if (!this.elementIdToScrollTo) {
            return;
        }
        try {
            var scrollingTargetElement = document.getElementById(this.elementIdToScrollTo);
            if (scrollingTargetElement == null) {
                return;
            }
            scrollingTargetElement.scrollIntoView();
        }
        finally {
            this.elementIdToScrollTo = null;
        }
    }
}