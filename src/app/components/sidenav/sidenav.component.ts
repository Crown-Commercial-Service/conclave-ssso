import { Component, ViewEncapsulation, ChangeDetectionStrategy, Input } from '@angular/core';
import { Router } from '@angular/router';
import { Observable } from 'rxjs';
import { slideAnimation } from 'src/app/animations/slide.animation';
import { AuthService } from 'src/app/services/auth/auth.service';

@Component({
    selector: 'app-sidenav',
    templateUrl: './sidenav.component.html',
    styleUrls: ['./sidenav.component.scss'],
    animations: [
        slideAnimation({
            close: { left: '-40px' },
            open: { left: '-16rem' }
        })
    ],
    encapsulation: ViewEncapsulation.None,
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class SideNavComponent {

    @Input() collapsed: boolean | null = true;

    constructor(private authService: AuthService) { }

    public logOut(): void {
        this.authService.logOutAndRedirect();
    }
    
    public isAuthenticated(): Observable<boolean> {
        return this.authService.isAuthenticated();
    }
}