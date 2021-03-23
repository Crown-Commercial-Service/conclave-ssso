import { animate, state, style, transition, trigger } from '@angular/animations';

export enum SlideAnimationStates {
    open = 'open',
    close = 'close'
}
export interface  ISlideAnimationStyles {
    close: any;
    open: any;
}
/**
 * @export
 * @param {ISlideAnimationStyles} slideStyle
 * @returns
 */
export function slideAnimation(slideStyle: ISlideAnimationStyles) {
    return trigger('slide', [
        state(SlideAnimationStates.close, style(slideStyle.close)),
        state(SlideAnimationStates.open, style(slideStyle.open)),
        transition('* => void', [
            animate(100, style({display:'none'}))
        ]),
        transition('close => open', [
            animate('500ms cubic-bezier(.3,0,0,1)'),
        ]),
        transition('open => close', [
            animate('500ms cubic-bezier(.3,0,0,1)'),
        ]),
    ])
}