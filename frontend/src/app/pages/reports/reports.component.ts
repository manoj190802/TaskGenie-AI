import { Component, OnInit } from '@angular/core';
import { ReportService } from '../../services/report.service';

@Component({
  selector: 'app-reports',
  templateUrl: './reports.component.html',
  styleUrls: ['./reports.component.css']
})
export class ReportsComponent implements OnInit {
  dashStats: any = {};
  projectsSummary: any = {};
  workloads: any[] = [];
  loading = true;

  constructor(private reportService: ReportService) {}

  ngOnInit(): void {
    this.loadAll();
  }

  loadAll(): void {
    this.reportService.getDashboardStats().subscribe({
      next: (d) => { this.dashStats = d; this.checkDone(); },
      error: () => this.checkDone()
    });
    this.reportService.getProjectsSummary().subscribe({
      next: (d) => { this.projectsSummary = d; this.checkDone(); },
      error: () => this.checkDone()
    });
    this.reportService.getDeveloperWorkload().subscribe({
      next: (d) => { this.workloads = d; this.checkDone(); },
      error: () => this.checkDone()
    });
  }

  private loaded = 0;
  checkDone(): void {
    this.loaded++;
    if (this.loaded >= 3) {
      this.loading = false;
    }
  }

  getStatusEntries(byStatus: any): { key: string; val: number }[] {
    if (!byStatus) return [];
    return Object.entries(byStatus).map(([key, val]) => ({ key, val: val as number }));
  }

  getPriorityEntries(byPriority: any): { key: string; val: number }[] {
    if (!byPriority) return [];
    return Object.entries(byPriority).map(([key, val]) => ({ key, val: val as number }));
  }

  getMax(entries: { val: number }[]): number {
    return Math.max(...entries.map(e => e.val), 1);
  }

  getWorkloadColor(pct: number): string {
    if (pct < 40) return 'var(--success)';
    if (pct < 70) return 'var(--warning)';
    return 'var(--accent)';
  }

  exportCSV(): void {
    const list = this.dashStats.recentProjects;
    if (!list?.length) { alert('No data to export.'); return; }
    const headers = ['Project Name', 'Client', 'Priority', 'Status', 'Created At'];
    const rows = list.map((p: any) => [
      p.projectName, p.clientName || 'Internal', p.priority || 'Medium', p.status,
      new Date(p.createdAt).toLocaleDateString()
    ]);
    const csv = [headers, ...rows].map(r => r.map((c: any) => `"${c}"`).join(',')).join('\n');
    const blob = new Blob([csv], { type: 'text/csv' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url; a.download = 'taskgenie-projects.csv'; a.click();
  }
}
