import { Component, OnInit } from '@angular/core';
import { ReportService } from '../../services/report.service';

@Component({
  selector: 'app-reports',
  templateUrl: './reports.component.html',
  styleUrls: ['./reports.component.css']
})
export class ReportsComponent implements OnInit {
  dashStats: any = {};
  taskSummary: any = {};
  workloads: any[] = [];
  aiStats: any = {};
  assignmentHistory: any = { data: [], total: 0 };
  loading = true;
  page = 1;

  constructor(private reportService: ReportService) {}

  ngOnInit(): void {
    this.loadAll();
  }

  loadAll(): void {
    this.reportService.getDashboardStats().subscribe(d => { this.dashStats = d; this.checkDone(); });
    this.reportService.getTaskSummary().subscribe(d => { this.taskSummary = d; this.checkDone(); });
    this.reportService.getDeveloperWorkload().subscribe(d => { this.workloads = d; this.checkDone(); });
    this.reportService.getAiStats().subscribe(d => { this.aiStats = d; this.checkDone(); });
    this.reportService.getAssignmentHistory(this.page).subscribe(d => { this.assignmentHistory = d; this.checkDone(); });
  }

  private loaded = 0;
  checkDone(): void { this.loaded++; if (this.loaded >= 5) this.loading = false; }

  loadPage(p: number): void {
    this.page = p;
    this.reportService.getAssignmentHistory(p).subscribe(d => this.assignmentHistory = d);
  }

  getStatusEntries(byStatus: any): { key: string; val: number }[] {
    if (!byStatus) return [];
    return Object.entries(byStatus).map(([key, val]) => ({ key, val: val as number }));
  }

  getCatEntries(byCat: any): { key: string; val: number }[] {
    if (!byCat) return [];
    return Object.entries(byCat).map(([key, val]) => ({ key, val: val as number }));
  }

  getMax(entries: { val: number }[]): number {
    return Math.max(...entries.map(e => e.val), 1);
  }

  getStatusBadge(s: string): string {
    return { Assigned:'badge-info', InProgress:'badge-warning', Completed:'badge-success',
      Cancelled:'badge-danger', Reassigned:'badge-muted' }[s] || 'badge-muted';
  }

  getWorkloadColor(pct: number): string {
    if (pct < 40) return 'var(--success)';
    if (pct < 70) return 'var(--warning)';
    return 'var(--accent)';
  }

  pages(): number[] {
    return Array.from({ length: this.assignmentHistory.totalPages || 0 }, (_, i) => i + 1);
  }

  exportCSV(): void {
    const assignments = this.assignmentHistory.data;
    if (!assignments?.length) { alert('No data to export.'); return; }
    const headers = ['Task', 'Project', 'Developer', 'AssignedBy', 'AssignedAt', 'Status', 'AIAssisted', 'AIScore'];
    const rows = assignments.map((a: any) => [
      a.taskTitle, a.projectName, a.developerName, a.assignedByName,
      new Date(a.assignedAt).toLocaleDateString(), a.status,
      a.aiAssisted ? 'Yes' : 'No', a.aiScore ? (a.aiScore * 100).toFixed(0) + '%' : '—'
    ]);
    const csv = [headers, ...rows].map(r => r.map((c: any) => `"${c}"`).join(',')).join('\n');
    const blob = new Blob([csv], { type: 'text/csv' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url; a.download = 'taskgenie-assignments.csv'; a.click();
  }
}
