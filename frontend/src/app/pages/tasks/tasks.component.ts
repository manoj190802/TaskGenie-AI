import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { TaskService } from '../../services/task.service';
import { TaskItem } from '../../models/models';

@Component({
  selector: 'app-tasks',
  templateUrl: './tasks.component.html',
  styleUrls: ['./tasks.component.css']
})
export class TasksComponent implements OnInit {
  tasks: TaskItem[] = [];
  loading = true;
  filterStatus = '';
  filterCategory = '';

  statuses = ['Pending', 'Assigned', 'InProgress', 'Completed', 'Cancelled'];
  categories = ['Frontend', 'Backend', 'Full Stack', 'Testing', 'DevOps', 'Design'];

  constructor(
    private taskService: TaskService,
    private router: Router,
    private route: ActivatedRoute,
  ) {}

  ngOnInit(): void {
    const projectId = this.route.snapshot.queryParams['projectId'];
    this.load(projectId);
  }

  load(projectId?: string): void {
    this.taskService.getAll(projectId, this.filterStatus || undefined, this.filterCategory || undefined)
      .subscribe({
        next: (data) => { this.tasks = data; this.loading = false; },
        error: () => { this.loading = false; }
      });
  }

  applyFilter(): void {
    this.loading = true;
    this.load();
  }

  viewTask(id: string): void { this.router.navigate(['/tasks', id]); }

  getStatusBadge(s: string): string {
    return { Pending:'badge-warning', Assigned:'badge-info', InProgress:'badge-primary',
      Completed:'badge-success', Cancelled:'badge-danger' }[s] || 'badge-muted';
  }
  getCatBadge(c: string): string {
    return { Frontend:'badge-primary', Backend:'badge-secondary', 'Full Stack':'badge-warning',
      Testing:'badge-success', DevOps:'badge-danger' }[c] || 'badge-muted';
  }
  getPriBadge(p: string): string {
    return { High:'badge-danger', Medium:'badge-warning', Low:'badge-muted' }[p] || 'badge-muted';
  }
}
