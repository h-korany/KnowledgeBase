import { Component, OnInit } from '@angular/core';
import { Router, RouterModule } from '@angular/router'; 
import { AuthService } from '../../services/auth.service';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule } from '@angular/forms';

// Angular Material Imports
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatIconModule } from '@angular/material/icon';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';

@Component({
  selector: 'app-login',
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.css'],
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatProgressSpinnerModule,
    MatIconModule,
    MatSnackBarModule,
    RouterModule
  ]
})
export class LoginComponent implements OnInit {
  loginForm: FormGroup;
  loading = false;
  hidePassword = true;

  constructor(
    private authService: AuthService,
    private router: Router,
    private fb: FormBuilder,
    private snackBar: MatSnackBar
  ) {
    this.loginForm = this.createForm();
  }

  ngOnInit(): void {
    console.log('LoginComponent initialized');
    
    // Check if user is already logged in
    if (this.authService.isLoggedIn()) {
      this.router.navigate(['/questionList']);
    }
  }

  private createForm(): FormGroup {
    return this.fb.group({
      username: ['', [
        Validators.required,
        Validators.email,
        Validators.minLength(3),
        Validators.maxLength(50)
      ]],
      password: ['', [
        Validators.required,
        Validators.minLength(6),
        Validators.maxLength(100)
      ]]
    });
  }

  login(): void {
    if (this.loginForm.invalid) {
      this.markFormGroupTouched();
      return;
    }

    this.loading = true;
    const { username, password } = this.loginForm.value;

    this.authService.login({ email: username, password }).subscribe({
      next: (response) => {
        this.loading = false;
        this.showSuccess('Login successful!');
        this.router.navigate(['/questionList']);
      },
      error: (error) => {
        this.loading = false;
        const errorMessage = this.getErrorMessage(error);
        this.showError(errorMessage);
        console.error('Login error:', error);
      }
    });
  }

  private markFormGroupTouched(): void {
    Object.keys(this.loginForm.controls).forEach(key => {
      this.loginForm.get(key)?.markAsTouched();
    });
  }

  private getErrorMessage(error: any): string {
    if (error.status === 401) {
      return 'Invalid username or password. Please try again.';
    } else if (error.status === 0) {
      return 'Unable to connect to server. Please check your connection.';
    } else if (error.status >= 500) {
      return 'Server error. Please try again later.';
    } else {
      return 'Login failed. Please check your credentials and try again.';
    }
  }

  private showSuccess(message: string): void {
    this.snackBar.open(message, 'Close', {
      duration: 3000,
      panelClass: ['success-snackbar']
    });
  }

  private showError(message: string): void {
    this.snackBar.open(message, 'Close', {
      duration: 5000,
      panelClass: ['error-snackbar']
    });
  }

  togglePasswordVisibility(): void {
    this.hidePassword = !this.hidePassword;
  }

  // Getters for template convenience
  get username() { return this.loginForm.get('username'); }
  get password() { return this.loginForm.get('password'); }

  getUsernameError(): string {
    if (this.username?.hasError('required')) return 'Email is required';
    if (this.username?.hasError('email')) return 'Please enter a valid email address';
    if (this.username?.hasError('minlength')) return 'Email must be at least 3 characters';
    if (this.username?.hasError('maxlength')) return 'Email must be less than 50 characters';
    return '';
  }

  getPasswordError(): string {
    if (this.password?.hasError('required')) return 'Password is required';
    if (this.password?.hasError('minlength')) return 'Password must be at least 6 characters';
    if (this.password?.hasError('maxlength')) return 'Password must be less than 100 characters';
    return '';
  }

  isFieldInvalid(fieldName: string): boolean {
    const control = this.loginForm.get(fieldName);
    return !!control && control.invalid && (control.dirty || control.touched);
  }

  // Demo credentials helper (remove in production)
  fillDemoCredentials(): void {
    this.loginForm.patchValue({
      username: 'demo@company.com',
      password: 'demo123'
    });
  }
}