import { Component, OnInit } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { ProjectService } from '../../services/project.service';
import { AssignmentService } from '../../services/assignment.service';
import { Project } from '../../models/models';

type WizardStep = 'details' | 'upload' | 'analyzing' | 'results';

@Component({
  selector: 'app-projects',
  templateUrl: './projects.component.html',
  styleUrls: ['./projects.component.css']
})
export class ProjectsComponent implements OnInit {
  projects: any[] = [];
  loading = true;
  showWizard = false;

  // ── Wizard State ──────────────────────────────────────────────────────────
  step: WizardStep = 'details';
  saving = false;
  wizardError = '';

  // Step 1 - Project Details
  form = {
    name: '',
    description: '',
    priority: 'Medium' as 'Low' | 'Medium' | 'High',
    clientName: '',
    dueDate: '',
  };

  // Step 2 - File Upload
  selectedFile: File | null = null;
  isDragOver = false;
  fileError = '';
  readonly ALLOWED_EXT = ['.pdf', '.docx', '.doc', '.txt'];
  readonly MAX_MB = 10;

  // Step 3 - Results
  wizardResult: any = null;
  assigningTaskId = '';
  assigningDeveloperId: Record<string, string> = {}; // taskId → developerId
  assignedTasks: Set<string> = new Set();
  assignmentNotes: Record<string, string> = {};

  constructor(
    private projectService: ProjectService,
    private assignmentService: AssignmentService,
    private router: Router,
    private route: ActivatedRoute,
  ) {}

  ngOnInit(): void {
    this.load();
    this.route.queryParams.subscribe(params => {
      if (params['new'] === 'true') {
        this.openWizard();
      }
    });
  }

  load(): void {
    this.projectService.getAll().subscribe({
      next: (data) => { this.projects = data; this.loading = false; },
      error: () => { this.loading = false; }
    });
  }

  // ── Wizard Control ─────────────────────────────────────────────────────────
  openWizard(): void {
    this.step = 'details';
    this.form = { name: '', description: '', priority: 'Medium', clientName: '', dueDate: '' };
    this.selectedFile = null;
    this.fileError = '';
    this.wizardError = '';
    this.wizardResult = null;
    this.assignedTasks = new Set();
    this.assigningDeveloperId = {};
    this.assignmentNotes = {};
    this.showWizard = true;
  }

  closeWizard(): void {
    if (this.step === 'analyzing') return; // don't close during analysis
    this.showWizard = false;
  }

  nextStep(): void {
    if (this.step === 'details') {
      if (!this.form.name.trim()) { this.wizardError = 'Project name is required.'; return; }
      this.wizardError = '';
      this.step = 'upload';
    }
  }

  backStep(): void {
    if (this.step === 'upload') this.step = 'details';
  }

  // ── File Handling ──────────────────────────────────────────────────────────
  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files?.length) this.setFile(input.files[0]);
  }

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
    if (files && files.length > 0) this.setFile(files[0]);
  }

  private setFile(file: File): void {
    this.fileError = '';
    const ext = '.' + file.name.split('.').pop()?.toLowerCase();
    if (!this.ALLOWED_EXT.includes(ext)) {
      this.fileError = `❌ "${ext}" not supported. Use PDF, DOCX, DOC, or TXT.`;
      this.selectedFile = null;
      return;
    }
    if (file.size / 1024 / 1024 > this.MAX_MB) {
      this.fileError = `❌ File too large (${(file.size / 1024 / 1024).toFixed(1)} MB). Max ${this.MAX_MB} MB.`;
      this.selectedFile = null;
      return;
    }
    this.selectedFile = file;
  }

  clearFile(): void { this.selectedFile = null; this.fileError = ''; }

  getFileIcon(): string {
    const ext = this.selectedFile?.name.split('.').pop()?.toLowerCase();
    if (ext === 'pdf') return '📕';
    if (ext === 'docx' || ext === 'doc') return '📘';
    if (ext === 'txt') return '📄';
    return '📁';
  }

  getFileSizeLabel(): string {
    if (!this.selectedFile) return '';
    const kb = this.selectedFile.size / 1024;
    return kb < 1024 ? `${kb.toFixed(1)} KB` : `${(kb / 1024).toFixed(1)} MB`;
  }

  // ── Submit Wizard ─────────────────────────────────────────────────────────
  submitWizard(): void {
    this.step = 'analyzing';
    this.wizardError = '';

    const fd = new FormData();
    fd.append('name', this.form.name.trim());
    fd.append('description', this.form.description.trim());
    fd.append('priority', this.form.priority);
    fd.append('clientName', this.form.clientName.trim());
    if (this.form.dueDate) fd.append('dueDate', this.form.dueDate);
    if (this.selectedFile) fd.append('file', this.selectedFile, this.selectedFile.name);

    this.projectService.createWithUpload(fd).subscribe({
      next: (res) => {
        this.wizardResult = res;
        // Initialize dev selections with top recommendation
        if (res.tasks?.length) {
          for (const task of res.tasks) {
            if (task.aiRecommendations?.length) {
              this.assigningDeveloperId[task.id] = task.aiRecommendations[0].developerId;
            }
          }
        }
        this.step = 'results';
        this.load();
      },
      error: (err) => {
        this.wizardError = err.error?.message || 'Failed to create project. Please try again.';
        this.step = 'upload';
      }
    });
  }

  // ── Task Assignment ────────────────────────────────────────────────────────
  assignTask(task: any): void {
    const developerId = this.assigningDeveloperId[task.id];
    if (!developerId) return;

    this.assigningTaskId = task.id;
    const rec = task.aiRecommendations?.find((r: any) => r.developerId === developerId);

    this.assignmentService.create({
      taskId: task.id,
      developerId,
      notes: this.assignmentNotes[task.id] || '',
      aiAssisted: !!rec,
      aiScore: rec?.score ?? null,
    }).subscribe({
      next: () => {
        this.assignedTasks.add(task.id);
        this.assigningTaskId = '';
      },
      error: (err) => {
        this.wizardError = err.error?.message || 'Assignment failed.';
        this.assigningTaskId = '';
      }
    });
  }

  isAssigned(taskId: string): boolean {
    return this.assignedTasks.has(taskId);
  }

  goToProject(): void {
    if (this.wizardResult?.projectId) {
      this.showWizard = false;
      this.router.navigate(['/projects', this.wizardResult.projectId]);
    }
  }

  // ── Display Helpers ────────────────────────────────────────────────────────
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

  getCatBadge(cat: string): string {
    const m: Record<string, string> = {
      Frontend: 'badge-primary', Backend: 'badge-secondary',
      'Full Stack': 'badge-warning', Testing: 'badge-success', DevOps: 'badge-danger'
    };
    return m[cat] || 'badge-muted';
  }

  getScoreColor(score: number): string {
    if (score >= 0.75) return 'var(--success)';
    if (score >= 0.5) return 'var(--warning)';
    return 'var(--accent)';
  }

  getScorePercent(score: number): number {
    return Math.round(score * 100);
  }

  get wizardStepIndex(): number {
    return { details: 1, upload: 2, analyzing: 3, results: 4 }[this.step] ?? 1;
  }
}
