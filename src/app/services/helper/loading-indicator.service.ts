import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs/internal/BehaviorSubject';
import { Observable } from 'rxjs/Observable';
import { ReplaySubject } from 'rxjs/ReplaySubject';
//import { BehaviorSubject } from 'rxjs/BehaviorSubject';

@Injectable({
    providedIn: 'root'
})
export class LoadingIndicatorService {
    // public isLoading = new BehaviorSubject<boolean>(false);
    //public isLoading: BehaviorSubject<boolean> = new BehaviorSubject(false);
    public isLoading: BehaviorSubject<boolean> = new BehaviorSubject<boolean>(true);

    constructor() {}
}


