import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-login',
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.css']
})
export class LoginComponent implements OnInit {
  form!: FormGroup;
  isRegister = false;
  loading = false;
  error = '';

  constructor(private fb: FormBuilder, private auth: AuthService, private router: Router) {
    this.buildForm();
  }

  ngOnInit(): void {
    if (this.auth.isLoggedIn()) { this.router.navigate(['/dashboard']); return; }
  }

  buildForm(): void {
    this.form = this.fb.group({
      name: [''],
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(6)]],
      role: ['ProjectManager'],
    });
  }

  toggle(): void {
    this.isRegister = !this.isRegister;
    this.error = '';
    if (this.isRegister) {
      this.form.get('name')!.setValidators([Validators.required, Validators.minLength(2)]);
    } else {
      this.form.get('name')!.clearValidators();
    }
    this.form.get('name')!.updateValueAndValidity();
  }

  submit(): void {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    this.loading = true;
    this.error = '';

    const { name, email, password, role } = this.form.value;
    const obs = this.isRegister
      ? this.auth.register(name, email, password, role)
      : this.auth.login(email, password);

    obs.subscribe({
      next: () => this.router.navigate(['/dashboard']),
      error: (err) => {
        this.error = err.error?.message || 'Authentication failed. Please try again.';
        this.loading = false;
      }
    });
  }
}
