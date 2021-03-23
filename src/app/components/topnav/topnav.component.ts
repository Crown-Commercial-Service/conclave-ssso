import { Component } from '@angular/core';
import { AuthService } from 'src/app/services/auth/auth.service';

@Component({
  selector: 'app-topnav',
  templateUrl: './topnav.component.html',
  styleUrls: ['./topnav.component.scss']
})
export class TopNavComponent {
  isAuthenticated: boolean = false;
  constructor(public authService: AuthService) {
  }

  ngOnInit() {
    this.isAuthenticated = this.authService.isUserAuthenticated();
  }

  signout() {
    this.authService.logOutAndRedirect();
  }
}
