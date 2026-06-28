import { Component, OnInit } from '@angular/core';
import { ReportService } from '../../services/report.service';

@Component({
  selector: 'app-dashboard',
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.css']
})
export class DashboardComponent implements OnInit {
  stats: any = null;
  workloadData: any[] = [];
  loading = true;
  kpiCards: any[] = [];

  constructor(private reportService: ReportService) {}

  ngOnInit(): void {
    this.loadDashboard();
  }

  loadDashboard(): void {
    this.reportService.getDashboardStats().subscribe({
      next: (stats) => {
        this.stats = stats;
        this.buildKpiCards();
        this.loading = false;
      },
      error: () => { this.loading = false; }
    });

    this.reportService.getDeveloperWorkload().subscribe({
      next: (data) => this.workloadData = data.slice(0, 5),
      error: () => {}
    });
  }

  buildKpiCards(): void {
    const s = this.stats;
    if (!s) return;

    this.kpiCards = [
      {
        label: 'Total Projects',
        value: s.totalProjects || 0,
        icon: '🗂️',
        gradient: 'linear-gradient(90deg, #6c63ff, #5a52d5)',
        sub: `${s.activeProjects || 0} Active`,
        color: '#6c63ff',
      },
      {
        label: 'Total Developers',
        value: s.totalDevelopers || 0,
        icon: '👨‍💻',
        gradient: 'linear-gradient(90deg, #00d9a6, #00b38a)',
        sub: `${s.availableDevelopers || 0} Available`,
        color: '#00d9a6',
      },
      {
        label: 'Active Projects',
        value: s.activeProjects || 0,
        icon: '🟢',
        gradient: 'linear-gradient(90deg, #51cf66, #40c057)',
        sub: 'Status: Active',
        color: '#51cf66',
      }
    ];
  }

  getWorkloadColor(pct: number): string {
    if (pct < 40) return '#51cf66';
    if (pct < 70) return '#ffa94d';
    return '#ff6b6b';
  }

  getStatusBadge(status: string): string {
    const map: Record<string, string> = {
      Active: 'badge-success', OnHold: 'badge-warning',
      Completed: 'badge-primary', Cancelled: 'badge-danger'
    };
    return map[status] || 'badge-muted';
  }

  getPriorityBadge(p: string): string {
    const m: Record<string, string> = { High: 'badge-danger', Medium: 'badge-warning', Low: 'badge-muted' };
    return m[p] || 'badge-muted';
  }
}
