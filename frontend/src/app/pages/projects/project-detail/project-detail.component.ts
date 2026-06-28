import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { ProjectService } from '../../../services/project.service';

@Component({
  selector: 'app-project-detail',
  templateUrl: './project-detail.component.html',
  styleUrls: ['./project-detail.component.css']
})
export class ProjectDetailComponent implements OnInit {
  projectId = '';
  project: any = null;
  loading = true;
  uploading = false;
  selectedFile: File | null = null;
  uploadMsg = '';
  error = '';

  // Recommendations state
  recommendations: any[] = [];
  recommendationsLoading = false;
  assigningDeveloperId = '';

  // Drag & Drop state
  isDragOver = false;

  readonly MAX_SIZE_MB = 10;
  readonly ALLOWED_EXT = ['.pdf', '.docx', '.doc', '.txt'];

  constructor(
    private route: ActivatedRoute,
    public router: Router,
    private projectService: ProjectService
  ) {}

  ngOnInit(): void {
    this.projectId = this.route.snapshot.params['id'];
    this.load();
  }

  load(): void {
    this.projectService.getById(this.projectId).subscribe({
      next: (data) => {
        this.project = data.project || data;
        this.loading = false;
        this.loadRecommendations();
      },
      error: () => {
        this.loading = false;
        this.error = 'Failed to load project.';
      }
    });
  }

  loadRecommendations(): void {
    if (!this.project?.requirementsText) {
      this.recommendations = [];
      return;
    }
    this.recommendationsLoading = true;
    this.projectService.getRecommendations(this.projectId).subscribe({
      next: (data) => {
        this.recommendations = data;
        this.recommendationsLoading = false;
      },
      error: () => {
        this.recommendationsLoading = false;
      }
    });
  }

  assignDeveloper(dev: any): void {
    if (!dev) return;
    this.assigningDeveloperId = dev.developerId;
    this.projectService.update(this.projectId, {
      assignedDeveloperId: dev.developerId,
      assignedDeveloperName: dev.name
    }).subscribe({
      next: (updatedProject) => {
        this.project = updatedProject;
        this.uploadMsg = `✅ Successfully assigned ${dev.name} to this project!`;
        this.assigningDeveloperId = '';
        setTimeout(() => this.uploadMsg = '', 5000);
      },
      error: (err) => {
        this.uploadMsg = `❌ Assignment failed: ${err.error?.message || 'Please try again.'}`;
        this.assigningDeveloperId = '';
      }
    });
  }

  unassignDeveloper(): void {
    this.projectService.update(this.projectId, {
      assignedDeveloperId: '',
      assignedDeveloperName: ''
    }).subscribe({
      next: (updatedProject) => {
        this.project = updatedProject;
        this.uploadMsg = `✅ Developer unassigned.`;
        setTimeout(() => this.uploadMsg = '', 3000);
      },
      error: () => {
        this.uploadMsg = `❌ Failed to unassign developer.`;
      }
    });
  }

  // ── File Selection ─────────────────────────────────────────────────────────
  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files?.length) {
      this.setFile(input.files[0]);
    }
  }

  // ── Drag & Drop ────────────────────────────────────────────────────────────
  onDragOver(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.isDragOver = true;
  }

  onDragLeave(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.isDragOver = false;
  }

  onDrop(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.isDragOver = false;
    const files = event.dataTransfer?.files;
    if (files && files.length > 0) {
      this.setFile(files[0]);
    }
  }

  // ── File Validation ────────────────────────────────────────────────────────
  private setFile(file: File): void {
    this.uploadMsg = '';
    const sizeMB = file.size / (1024 * 1024);
    const ext = '.' + file.name.split('.').pop()?.toLowerCase();

    if (!this.ALLOWED_EXT.includes(ext)) {
      this.uploadMsg = `❌ Invalid file type "${ext}". Only PDF, DOCX, DOC, TXT allowed.`;
      this.selectedFile = null;
      return;
    }
    if (sizeMB > this.MAX_SIZE_MB) {
      this.uploadMsg = `❌ File too large (${sizeMB.toFixed(1)} MB). Max ${this.MAX_SIZE_MB} MB allowed.`;
      this.selectedFile = null;
      return;
    }
    this.selectedFile = file;
  }

  getFileIcon(): string {
    if (!this.selectedFile) return '📁';
    const ext = this.selectedFile.name.split('.').pop()?.toLowerCase();
    if (ext === 'pdf') return '📕';
    if (ext === 'docx' || ext === 'doc') return '📘';
    if (ext === 'txt') return '📄';
    return '📁';
  }

  getFileSizeLabel(): string {
    if (!this.selectedFile) return '';
    const kb = this.selectedFile.size / 1024;
    if (kb < 1024) return `${kb.toFixed(1)} KB`;
    return `${(kb / 1024).toFixed(1)} MB`;
  }

  clearFile(): void {
    this.selectedFile = null;
    this.uploadMsg = '';
  }

  // ── Upload ─────────────────────────────────────────────────────────────────
  uploadRequirements(): void {
    if (!this.selectedFile) {
      this.uploadMsg = 'Please select a file first.';
      return;
    }
    this.uploading = true;
    this.uploadMsg = '';

    this.projectService.uploadRequirements(this.projectId, this.selectedFile).subscribe({
      next: (res) => {
        this.uploadMsg = `✅ Successfully uploaded & extracted text from ${res.fileName}`;
        this.uploading = false;
        this.selectedFile = null;
        this.load();
      },
      error: (err) => {
        this.uploadMsg = `❌ ${err.error?.message || 'Upload failed'}`;
        this.uploading = false;
      }
    });
  }

  // ── Status Badges ──────────────────────────────────────────────────────────
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

  getScoreColor(score: number): string {
    if (score >= 80) return 'var(--success)';
    if (score >= 50) return 'var(--warning)';
    return 'var(--accent)';
  }
}
