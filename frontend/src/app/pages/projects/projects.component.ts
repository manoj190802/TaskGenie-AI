import { Component, OnInit } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { ProjectService } from '../../services/project.service';
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
  activeTab: 'analysis' | 'projects' = 'analysis';

  // ── Tab 1: Project Analysis State ──────────────────────────────────────────
  analysisFile: File | null = null;
  analysisDragOver = false;
  analysisFileError = '';
  analysisLoading = false;
  analysisError = '';
  analysisResult: any = null;
  analysisProjectName = '';
  assigningDeveloperId = '';
  assignmentSuccessMsg = '';

  // ── Tab 2: Wizard State (Project Creation) ───────────────────────────────
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

  constructor(
    private projectService: ProjectService,
    private router: Router,
    private route: ActivatedRoute,
  ) {}

  ngOnInit(): void {
    this.load();
    this.route.queryParams.subscribe(params => {
      if (params['new'] === 'true') {
        this.activeTab = 'projects';
        this.openWizard();
      }
    });
  }

  load(): void {
    this.projectService.getAll().subscribe({
      next: (data) => {
        this.projects = data.map(p => {
          if (p.project) return p;
          return {
            project: p,
            taskCounts: { total: 0, pending: 0, assigned: 0, completed: 0 }
          };
        }).filter((item: any) => item.project.status === 'Active');
        this.loading = false;
      },
      error: () => { this.loading = false; }
    });
  }

  completeProject(project: any, event: Event): void {
    event.stopPropagation();
    if (!confirm(`Are you sure you want to mark project "${project.name}" as Completed?`)) return;

    this.projectService.update(project.id, { status: 'Completed' }).subscribe({
      next: () => {
        this.assignmentSuccessMsg = `✅ Project "${project.name}" completed successfully!`;
        this.load();
        setTimeout(() => this.assignmentSuccessMsg = '', 5000);
      },
      error: (err) => {
        this.analysisError = err.error?.message || 'Failed to complete project.';
      }
    });
  }

  switchTab(tab: 'analysis' | 'projects'): void {
    this.activeTab = tab;
  }

  // ── Project Analysis File Handlers ─────────────────────────────────────────
  onAnalysisFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files?.length) this.setAnalysisFile(input.files[0]);
  }

  onAnalysisDragOver(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.analysisDragOver = true;
  }

  onAnalysisDragLeave(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.analysisDragOver = false;
  }

  onAnalysisDrop(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.analysisDragOver = false;
    const files = event.dataTransfer?.files;
    if (files && files.length > 0) this.setAnalysisFile(files[0]);
  }

  private setAnalysisFile(file: File): void {
    this.analysisFileError = '';
    this.analysisResult = null;
    this.analysisError = '';
    const ext = '.' + file.name.split('.').pop()?.toLowerCase();
    if (!this.ALLOWED_EXT.includes(ext)) {
      this.analysisFileError = `❌ "${ext}" not supported. Use PDF, DOCX, DOC, or TXT.`;
      this.analysisFile = null;
      return;
    }
    if (file.size / 1024 / 1024 > this.MAX_MB) {
      this.analysisFileError = `❌ File too large (${(file.size / 1024 / 1024).toFixed(1)} MB). Max ${this.MAX_MB} MB.`;
      this.analysisFile = null;
      return;
    }
    this.analysisFile = file;
  }

  clearAnalysisFile(): void {
    this.analysisFile = null;
    this.analysisFileError = '';
    this.analysisResult = null;
    this.analysisError = '';
    this.analysisProjectName = '';
    this.assigningDeveloperId = '';
    this.assignmentSuccessMsg = '';
  }

  getAnalysisFileIcon(): string {
    const ext = this.analysisFile?.name.split('.').pop()?.toLowerCase();
    if (ext === 'pdf') return '📕';
    if (ext === 'docx' || ext === 'doc') return '📘';
    if (ext === 'txt') return '📄';
    return '📁';
  }

  getAnalysisFileSizeLabel(): string {
    if (!this.analysisFile) return '';
    const kb = this.analysisFile.size / 1024;
    return kb < 1024 ? `${kb.toFixed(1)} KB` : `${(kb / 1024).toFixed(1)} MB`;
  }

  runAnalysis(): void {
    if (!this.analysisFile) return;
    this.analysisLoading = true;
    this.analysisError = '';
    this.analysisResult = null;
    this.assignmentSuccessMsg = '';

    this.projectService.analyzeDocument(this.analysisFile).subscribe({
      next: (res) => {
        this.analysisResult = res;
        this.analysisLoading = false;
      },
      error: (err) => {
        this.analysisError = err.error?.message || 'Failed to analyze requirements document. Please try again.';
        this.analysisLoading = false;
      }
    });
  }

  assignDeveloper(dev: any): void {
    if (!this.analysisFile) return;
    this.assigningDeveloperId = dev.developerId;
    this.analysisError = '';
    this.assignmentSuccessMsg = '';

    const projectName = this.analysisProjectName.trim() || `Project - ${this.analysisFile.name.split('.')[0]}`;

    const fd = new FormData();
    fd.append('name', projectName);
    fd.append('description', `Analyzed requirements: ${this.analysisFile.name}`);
    fd.append('priority', 'Medium');
    fd.append('clientName', 'AI Matched Client');
    fd.append('assignedDeveloperId', dev.developerId || '');
    fd.append('assignedDeveloperName', dev.name);
    fd.append('file', this.analysisFile, this.analysisFile.name);

    this.projectService.createWithUpload(fd).subscribe({
      next: () => {
        this.assignmentSuccessMsg = `🎉 Project "${projectName}" assigned successfully to ${dev.name}!`;
        this.assigningDeveloperId = '';
        this.load();
        setTimeout(() => this.assignmentSuccessMsg = '', 6000);
      },
      error: (err) => {
        this.analysisError = err.error?.message || 'Failed to create and assign project. Please try again.';
        this.assigningDeveloperId = '';
      }
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
    this.showWizard = true;
  }

  closeWizard(): void {
    if (this.step === 'analyzing') return;
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

  // ── File Handling (Wizard) ────────────────────────────────────────────────
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
        this.step = 'results';
        this.load();
      },
      error: (err) => {
        this.wizardError = err.error?.message || 'Failed to create project. Please try again.';
        this.step = 'upload';
      }
    });
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

  getScoreColor(score: number): string {
    if (score >= 80) return 'var(--success)';
    if (score >= 50) return 'var(--warning)';
    return 'var(--accent)';
  }

  get wizardStepIndex(): number {
    return { details: 1, upload: 2, analyzing: 3, results: 4 }[this.step] ?? 1;
  }
}
