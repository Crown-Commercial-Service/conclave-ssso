import { ChangeDetectionStrategy, Component, OnInit, ViewEncapsulation } from '@angular/core';
import { Store } from '@ngrx/store';
import { timeout } from 'rxjs/operators';
import { slideAnimation } from 'src/app/animations/slide.animation';

import { BaseComponent } from 'src/app/components/base/base.component';
import { Data } from 'src/app/models/data';
import { dataService } from 'src/app/services/data/data.service';
import { UIState } from 'src/app/store/ui.states';

@Component({
    selector: 'app-token',
    templateUrl: './token.component.html',
    styleUrls: ['./token.component.scss'],
    animations: [
        slideAnimation({
            close: { 'transform': 'translateX(12.5rem)' },
            open: { left: '-12.5rem' }
        })
    ],
    encapsulation: ViewEncapsulation.None,
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class TokenComponent extends BaseComponent implements OnInit {

    // sampleData: Data[] = [];
    sampleData!: string | null;

    constructor(private dataService: dataService, protected uiStore: Store<UIState>) {
        super(uiStore);
    }

    ngOnInit() {
        // this.dataService.getData().then(
        //     response => {
        //         response.subscribe(res => {
        //             this.sampleData = res;
        //         });
        //     },
        //     err => console.log(err)
        // );
        this.sampleData = localStorage.getItem('brickedon_aws_tokens');
    }
}
