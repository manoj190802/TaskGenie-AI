import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { TaskService } from '../../../services/task.service';
import { AssignmentService } from '../../../services/assignment.service';
import { DeveloperService } from '../../../services/developer.service';
import { TaskItem, MatchResult, Developer } from '../../../models/models';

@Component({
  selector: 'app-task-detail',
  templateUrl: './task-detail.component.html',
  styleUrls: ['./task-detail.component.css']
})
export class TaskDetailComponent implements OnInit {
  task?: TaskItem;
  matchResult?: MatchResult;
  developers: Developer[] = [];
  loading = true;
  loadingRecs = false;
  assigning = false;
  assignMsg = '';
  error = '';
  selectedDeveloperId = '';
  showReassignModal = false;
  reassignReason = '';
  currentAssignmentId = '';

  constructor(
    private route: ActivatedRoute,
    public router: Router,
    private taskService: TaskService,
    private assignmentService: AssignmentService,
    private developerService: DeveloperService,
  ) {}

  ngOnInit(): void {
    const id = this.route.snapshot.params['id'];
    this.taskService.getById(id).subscribe({
      next: (t) => {
        this.task = t;
        this.loading = false;
        if (t.aiRecommendations?.length) {
          // Reconstruct match result from stored recs
          this.matchResult = {
            taskTitle: t.title,
            recommendations: t.aiRecommendations.map(r => ({
              developerId: r.developerId,
              developerName: r.developerName,
              score: r.score,
              skillMatchScore: r.score,
              experienceScore: 0,
              availabilityScore: 0,
              workloadScore: 0,
              matchedSkills: r.matchedSkills,
              missingSkills: r.missingSkills,
              recommendationReason: r.reason,
            })),
            bestMatch: t.aiRecommendations[0] ? {
              developerId: t.aiRecommendations[0].developerId,
              developerName: t.aiRecommendations[0].developerName,
              score: t.aiRecommendations[0].score,
              skillMatchScore: t.aiRecommendations[0].score,
              experienceScore: 0, availabilityScore: 0, workloadScore: 0,
              matchedSkills: t.aiRecommendations[0].matchedSkills,
              missingSkills: t.aiRecommendations[0].missingSkills,
              recommendationReason: t.aiRecommendations[0].reason,
            } : undefined,
          };
        }
      },
      error: () => { this.loading = false; this.error = 'Task not found.'; }
    });
    this.developerService.getAll().subscribe(d => this.developers = d);
  }

  getRecommendations(): void {
    this.loadingRecs = true;
    this.taskService.getRecommendations(this.task!.id!).subscribe({
      next: (res) => {
        this.matchResult = res;
        this.loadingRecs = false;
        // Refresh task to get stored recs
        this.taskService.getById(this.task!.id!).subscribe(t => this.task = t);
      },
      error: (err) => {
        this.error = err.error?.message || 'Failed to get recommendations. Make sure AI service is running.';
        this.loadingRecs = false;
      }
    });
  }

  assignDeveloper(devId: string, aiScore?: number): void {
    this.assigning = true;
    this.assignmentService.create({
      taskId: this.task!.id!,
      developerId: devId,
      notes: 'Assigned via AI recommendation',
      aiAssisted: !!aiScore,
      aiScore: aiScore,
    }).subscribe({
      next: () => {
        this.assignMsg = '✅ Developer assigned successfully!';
        this.assigning = false;
        this.taskService.getById(this.task!.id!).subscribe(t => this.task = t);
      },
      error: (err) => {
        this.assignMsg = `❌ ${err.error?.message || 'Assignment failed'}`;
        this.assigning = false;
      }
    });
  }

  updateStatus(status: string): void {
    this.taskService.updateStatus(this.task!.id!, status).subscribe({
      next: () => { this.task!.status = status as any; },
    });
  }

  getScorePct(score: number): number { return Math.round(score * 100); }

  getScoreColor(score: number): string {
    if (score >= 0.7) return 'var(--success)';
    if (score >= 0.45) return 'var(--warning)';
    return 'var(--accent)';
  }

  getStatusBadge(s: string): string {
    return { Pending:'badge-warning', Assigned:'badge-info', InProgress:'badge-primary',
      Completed:'badge-success', Cancelled:'badge-danger' }[s] || 'badge-muted';
  }
  getCatBadge(c: string): string {
    return { Frontend:'badge-primary', Backend:'badge-secondary',
      'Full Stack':'badge-warning', Testing:'badge-success', DevOps:'badge-danger' }[c] || 'badge-muted';
  }
}
