import { Component } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-sidebar',
  templateUrl: './sidebar.component.html',
  styleUrls: ['./sidebar.component.css']
})
export class SidebarComponent {
  navItems = [
    { path: '/dashboard', icon: '📊', label: 'Dashboard' },
    { path: '/projects', icon: '🗂️', label: 'Projects' },
    { path: '/developers', icon: '👨‍💻', label: 'Developers' },
    { path: '/assignments', icon: '🔗', label: 'Assignments' },
    { path: '/reports', icon: '📈', label: 'Reports' },
  ];

  constructor(public authService: AuthService, public router: Router) {}

  logout() { this.authService.logout(); }

  isActive(path: string): boolean {
    return this.router.url.startsWith(path);
  }
}
