import { Component, OnInit } from '@angular/core';
import { AssignmentService } from '../../services/assignment.service';
import { DeveloperService } from '../../services/developer.service';
import { Assignment, Developer } from '../../models/models';

@Component({
  selector: 'app-assignments',
  templateUrl: './assignments.component.html',
  styleUrls: ['./assignments.component.css']
})
export class AssignmentsComponent implements OnInit {
  assignments: Assignment[] = [];
  developers: Developer[] = [];
  loading = true;
  filterStatus = '';
  expandedId = '';
  showReassign = false;
  reassignTarget?: Assignment;
  newDevId = '';
  reason = '';
  reassigning = false;
  reassignMsg = '';

  statuses = ['Assigned', 'InProgress', 'Completed', 'Cancelled', 'Reassigned'];

  constructor(
    private assignmentService: AssignmentService,
    private developerService: DeveloperService,
  ) {}

  ngOnInit(): void {
    this.load();
    this.developerService.getAll().subscribe(d => this.developers = d);
  }

  load(): void {
    this.assignmentService.getAll(undefined, undefined, this.filterStatus || undefined).subscribe({
      next: (data) => { this.assignments = data; this.loading = false; },
      error: () => { this.loading = false; }
    });
  }

  applyFilter(): void { this.loading = true; this.load(); }

  toggleExpand(id: string): void { this.expandedId = this.expandedId === id ? '' : id; }

  openReassign(a: Assignment): void {
    this.reassignTarget = a;
    this.newDevId = '';
    this.reason = '';
    this.reassignMsg = '';
    this.showReassign = true;
  }

  closeReassign(): void { this.showReassign = false; this.reassignMsg = ''; }

  confirmReassign(): void {
    if (!this.newDevId || !this.reason.trim()) {
      this.reassignMsg = 'Please select a developer and provide a reason.';
      return;
    }
    this.reassigning = true;
    this.assignmentService.reassign(this.reassignTarget!.id!, this.newDevId, this.reason).subscribe({
      next: () => { this.closeReassign(); this.load(); this.reassigning = false; },
      error: (err) => {
        this.reassignMsg = err.error?.message || 'Reassignment failed.';
        this.reassigning = false;
      }
    });
  }

  updateStatus(id: string, status: string): void {
    this.assignmentService.updateStatus(id, status).subscribe(() => this.load());
  }

  getStatusBadge(s: string): string {
    return { Assigned:'badge-info', InProgress:'badge-warning', Completed:'badge-success',
      Cancelled:'badge-danger', Reassigned:'badge-muted' }[s] || 'badge-muted';
  }
}
