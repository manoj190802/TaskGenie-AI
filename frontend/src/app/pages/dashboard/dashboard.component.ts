import { Component, OnInit } from '@angular/core';
import { ReportService } from '../../services/report.service';
import { DashboardStats } from '../../models/models';

@Component({
  selector: 'app-dashboard',
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.css']
})
export class DashboardComponent implements OnInit {
  stats?: DashboardStats;
  workloadData: any[] = [];
  aiStats: any = {};
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

    this.reportService.getAiStats().subscribe({
      next: (data) => this.aiStats = data,
      error: () => {}
    });

    this.reportService.getDeveloperWorkload().subscribe({
      next: (data) => this.workloadData = data.slice(0, 5),
      error: () => {}
    });
  }

  buildKpiCards(): void {
    const s = this.stats!;
    this.kpiCards = [
      {
        label: 'Total Projects',
        value: s.totalProjects,
        icon: '🗂️',
        gradient: 'linear-gradient(90deg, #6c63ff, #5a52d5)',
        sub: `${s.activeProjects} Active`,
        color: '#6c63ff',
      },
      {
        label: 'Pending Tasks',
        value: s.pendingTasks,
        icon: '⏳',
        gradient: 'linear-gradient(90deg, #ffa94d, #fd7e14)',
        sub: `${s.totalTasks} Total`,
        color: '#ffa94d',
      },
      {
        label: 'Assigned Tasks',
        value: s.assignedTasks,
        icon: '🔗',
        gradient: 'linear-gradient(90deg, #4dabf7, #228be6)',
        sub: `${s.inProgressTasks} In Progress`,
        color: '#4dabf7',
      },
      {
        label: 'Completed Tasks',
        value: s.completedTasks,
        icon: '✅',
        gradient: 'linear-gradient(90deg, #51cf66, #40c057)',
        sub: `${s.totalTasks > 0 ? Math.round(s.completedTasks / s.totalTasks * 100) : 0}% Done`,
        color: '#51cf66',
      },
      {
        label: 'Total Developers',
        value: s.totalDevelopers,
        icon: '👨‍💻',
        gradient: 'linear-gradient(90deg, #00d9a6, #00b38a)',
        sub: `${s.availableDevelopers} Available`,
        color: '#00d9a6',
      },
      {
        label: 'Total Assignments',
        value: s.totalAssignments,
        icon: '📋',
        gradient: 'linear-gradient(90deg, #ff6b6b, #e03131)',
        sub: 'All Time',
        color: '#ff6b6b',
      },
    ];
  }

  getStatusBadge(status: string): string {
    const map: Record<string, string> = {
      'Assigned': 'badge-info',
      'InProgress': 'badge-warning',
      'Completed': 'badge-success',
      'Cancelled': 'badge-danger',
    };
    return map[status] || 'badge-muted';
  }

  getCategoryBadge(cat: string): string {
    const map: Record<string, string> = {
      'Frontend': 'badge-primary',
      'Backend': 'badge-secondary',
      'Full Stack': 'badge-warning',
      'Testing': 'badge-success',
      'DevOps': 'badge-danger',
    };
    return map[cat] || 'badge-muted';
  }

  getWorkloadColor(pct: number): string {
    if (pct < 40) return '#51cf66';
    if (pct < 70) return '#ffa94d';
    return '#ff6b6b';
  }

  trackByCategory(index: number, item: any) { return item.category; }
  trackByAssignment(index: number, item: any) { return item.assignmentId; }
}
