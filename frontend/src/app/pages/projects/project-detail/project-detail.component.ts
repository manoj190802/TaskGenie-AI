import { Component, OnInit, HostListener } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { ProjectService } from '../../../services/project.service';
import { TaskService } from '../../../services/task.service';

@Component({
  selector: 'app-project-detail',
  templateUrl: './project-detail.component.html',
  styleUrls: ['./project-detail.component.css']
})
export class ProjectDetailComponent implements OnInit {
  projectId = '';
  project: any = null;
  tasks: any[] = [];
  loading = true;
  uploading = false;
  analyzing = false;
  selectedFile: File | null = null;
  uploadMsg = '';
  analyzeMsg = '';
  error = '';

  // Drag & Drop state
  isDragOver = false;
  isDragActive = false; // page-level drag indicator

  readonly MAX_SIZE_MB = 10;
  readonly ALLOWED_TYPES = [
    'application/pdf',
    'application/vnd.openxmlformats-officedocument.wordprocessingml.document',
    'application/msword',
    'text/plain',
  ];
  readonly ALLOWED_EXT = ['.pdf', '.docx', '.doc', '.txt'];

  constructor(
    private route: ActivatedRoute,
    public router: Router,
    private projectService: ProjectService,
    private taskService: TaskService,
  ) {}

  ngOnInit(): void {
    this.projectId = this.route.snapshot.params['id'];
    this.load();
  }

  load(): void {
    this.projectService.getById(this.projectId).subscribe({
      next: (data) => {
        this.project = data.project;
        this.tasks = data.tasks || [];
        this.loading = false;
      },
      error: () => { this.loading = false; this.error = 'Failed to load project.'; }
    });
  }

  // ── File Selection via input ───────────────────────────────────────────────
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
    if (event.dataTransfer) {
      event.dataTransfer.dropEffect = 'copy';
    }
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
    if (!this.selectedFile) { this.uploadMsg = 'Please select a file first.'; return; }
    this.uploading = true;
    this.projectService.uploadRequirements(this.projectId, this.selectedFile).subscribe({
      next: (res) => {
        this.uploadMsg = `✅ Extracted ${res.wordCount} words from ${res.fileName}`;
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

  // ── AI Analysis ────────────────────────────────────────────────────────────
  analyzeRequirements(): void {
    this.analyzing = true;
    this.analyzeMsg = '';
    this.projectService.analyzeRequirements(this.projectId).subscribe({
      next: (res) => {
        this.analyzeMsg = `✅ ${res.message}`;
        this.analyzing = false;
        this.load();
      },
      error: (err) => {
        this.analyzeMsg = `❌ ${err.error?.message || 'Analysis failed. Make sure the AI service is running.'}`;
        this.analyzing = false;
      }
    });
  }

  viewTask(taskId: string): void {
    this.router.navigate(['/tasks', taskId]);
  }

  getStatusBadge(status: string): string {
    const m: Record<string,string> = {
      Pending:'badge-warning', Assigned:'badge-info',
      InProgress:'badge-primary', Completed:'badge-success', Cancelled:'badge-danger'
    };
    return m[status] || 'badge-muted';
  }

  getCatBadge(cat: string): string {
    const m: Record<string,string> = {
      Frontend:'badge-primary', Backend:'badge-secondary',
      'Full Stack':'badge-warning', Testing:'badge-success', DevOps:'badge-danger'
    };
    return m[cat] || 'badge-muted';
  }

  getPriorityBadge(p: string): string {
    const m: Record<string,string> = { High:'badge-danger', Medium:'badge-warning', Low:'badge-muted' };
    return m[p] || 'badge-muted';
  }
}
