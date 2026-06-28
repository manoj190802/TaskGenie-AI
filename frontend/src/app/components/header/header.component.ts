import { Component } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-header',
  templateUrl: './header.component.html',
  styleUrls: ['./header.component.css']
})
export class HeaderComponent {
  constructor(public authService: AuthService, public router: Router) {}

  getPageTitle(): string {
    const url = this.router.url;
    if (url.includes('/dashboard')) return 'Dashboard';
    if (url.includes('/projects')) return 'Projects';
    if (url.includes('/tasks')) return 'Tasks';
    if (url.includes('/developers')) return 'Developers';
    if (url.includes('/assignments')) return 'Assignments';
    if (url.includes('/reports')) return 'Reports';
    return 'TaskGenie AI';
  }
}
