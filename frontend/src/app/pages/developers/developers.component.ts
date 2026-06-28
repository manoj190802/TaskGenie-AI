import { Component, OnInit } from '@angular/core';
import { DeveloperService } from '../../services/developer.service';
import { Developer } from '../../models/models';

@Component({
  selector: 'app-developers',
  templateUrl: './developers.component.html',
  styleUrls: ['./developers.component.css']
})
export class DevelopersComponent implements OnInit {
  developers: Developer[] = [];
  loading = true;
  showModal = false;
  saving = false;
  error = '';
  editMode = false;
  editId = '';
  skillInput = '';

  newDev: Partial<Developer> = {
    name: '', email: '', skills: [], experienceYears: 0,
    availability: 'Available', currentWorkload: 0, bio: ''
  };

  constructor(private devService: DeveloperService) {}

  ngOnInit(): void { this.load(); }

  load(): void {
    this.devService.getAll().subscribe({
      next: (d) => { this.developers = d; this.loading = false; },
      error: () => { this.loading = false; }
    });
  }

  openModal(dev?: Developer): void {
    if (dev) {
      this.editMode = true;
      this.editId = dev.id!;
      this.newDev = { ...dev };
    } else {
      this.editMode = false;
      this.editId = '';
      this.newDev = { name: '', email: '', skills: [], experienceYears: 0, availability: 'Available', currentWorkload: 0, bio: '' };
    }
    this.showModal = true;
    this.error = '';
    this.skillInput = '';
  }

  closeModal(): void { this.showModal = false; this.error = ''; }

  addSkill(): void {
    const s = this.skillInput.trim();
    if (s && !this.newDev.skills?.includes(s)) {
      this.newDev.skills = [...(this.newDev.skills || []), s];
    }
    this.skillInput = '';
  }

  removeSkill(skill: string): void {
    this.newDev.skills = this.newDev.skills?.filter(s => s !== skill);
  }

  onSkillKeydown(e: KeyboardEvent): void {
    if (e.key === 'Enter' || e.key === ',') { e.preventDefault(); this.addSkill(); }
  }

  save(): void {
    if (!this.newDev.name?.trim()) { this.error = 'Name is required.'; return; }
    this.saving = true;
    const obs = this.editMode
      ? this.devService.update(this.editId, this.newDev)
      : this.devService.create(this.newDev);

    obs.subscribe({
      next: () => { this.closeModal(); this.load(); this.saving = false; },
      error: (err) => { this.error = err.error?.message || 'Failed to save.'; this.saving = false; }
    });
  }

  delete(id: string): void {
    if (!confirm('Deactivate this developer?')) return;
    this.devService.delete(id).subscribe(() => this.load());
  }

  getAvailBadge(a: string): string {
    return { Available:'badge-success', Busy:'badge-warning', OnLeave:'badge-danger' }[a] || 'badge-muted';
  }

  getWorkloadColor(pct: number): string {
    if (pct <= 1) return 'var(--success)';
    if (pct === 2) return 'var(--warning)';
    return 'var(--accent)';
  }
}
