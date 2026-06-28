import { Component } from '@angular/core';
import { AuthService } from './services/auth.service';

@Component({
  selector: 'app-root',
  template: `
    <div class="app-layout" *ngIf="authService.isLoggedIn(); else loginView">
      <app-sidebar></app-sidebar>
      <div class="main-content">
        <app-header></app-header>
        <div class="page-container">
          <router-outlet></router-outlet>
        </div>
      </div>
    </div>
    <ng-template #loginView>
      <router-outlet></router-outlet>
    </ng-template>
  `,
})
export class AppComponent {
  constructor(public authService: AuthService) {}
}
