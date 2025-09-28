import { Injectable, Inject, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { BehaviorSubject, Observable, throwError } from 'rxjs';
import { tap, catchError } from 'rxjs/operators';
import { Router } from '@angular/router';
import { LoginModel, AuthResponse, CurrentUser, RegisterModel } from '../models/user.model';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly API_URL = 'https://localhost:44353/api/user';
  private currentUserSubject = new BehaviorSubject<CurrentUser | null>(null);
  private tokenExpirationTimer: any;
  private isBrowser: boolean;

  constructor(
    @Inject(PLATFORM_ID) private platformId: any,
    private http: HttpClient,
    private router: Router
  ) {
    this.isBrowser = isPlatformBrowser(this.platformId);
    this.initializeAuthState();
  }

  /**
   * Initialize authentication state from localStorage (browser only)
   */
  private initializeAuthState(): void {
    if (!this.isBrowser) return;

    const userJson = localStorage.getItem('currentUser');
    const token = localStorage.getItem('token');
    const tokenExpiration = localStorage.getItem('tokenExpiration');

    if (userJson && token && tokenExpiration) {
      const user = JSON.parse(userJson);
      const expirationDate = new Date(tokenExpiration);
      
      // Check if token is still valid
      if (expirationDate > new Date()) {
        this.currentUserSubject.next(user);
        this.setAutoLogout(expirationDate.getTime() - new Date().getTime());
      } else {
        this.clearAuthData();
      }
    }
  }

  /**
   * User registration
   */
  register(registerModel: RegisterModel): Observable<any> {
    const payload = {
      ...registerModel,
    };
    
    return this.http.post(`${this.API_URL}/Register`, payload).pipe(
      catchError(this.handleError)
    );
  }

  /**
   * User login with enhanced error handling
   */
  login(loginModel: LoginModel): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.API_URL}/Login`, loginModel).pipe(
      tap(response => {
        this.handleAuthentication(response);
      }),
      catchError(this.handleError)
    );
  }

  /**
   * Handle successful authentication
   */
  private handleAuthentication(response: AuthResponse): void {
    const expirationDate = new Date(new Date().getTime() + 24 * 60 * 60 * 1000);
    
    const currentUser: CurrentUser = {
      id: response.userId,
      userName: response.userName,
      email: response.email,
      roles: response.roles || ['user'],
      loginTime: new Date()
    };

    // Store auth data (browser only)
    if (this.isBrowser) {
      localStorage.setItem('token', response.token);
      localStorage.setItem('currentUser', JSON.stringify(currentUser));
      localStorage.setItem('tokenExpiration', expirationDate.toISOString());
    }

    this.currentUserSubject.next(currentUser);
    this.setAutoLogout(24 * 60 * 60 * 1000);
  }

  /**
   * Auto logout when token expires
   */
  private setAutoLogout(expirationDuration: number): void {
    if (!this.isBrowser) return;

    if (this.tokenExpirationTimer) {
      clearTimeout(this.tokenExpirationTimer);
    }

    this.tokenExpirationTimer = setTimeout(() => {
      this.logout();
      this.router.navigate(['/login'], { 
        queryParams: { sessionExpired: true } 
      });
    }, expirationDuration);
  }

  /**
   * User logout with cleanup
   */
  logout(): void {
    this.clearAuthData();
    this.currentUserSubject.next(null);
    
    if (this.isBrowser && this.tokenExpirationTimer) {
      clearTimeout(this.tokenExpirationTimer);
    }

    this.router.navigate(['/login']);
    console.log('User logged out successfully');
  }

  /**
   * Clear all authentication data from storage
   */
  private clearAuthData(): void {
    if (!this.isBrowser) return;

    localStorage.removeItem('token');
    localStorage.removeItem('currentUser');
    localStorage.removeItem('tokenExpiration');
  }

  /**
   * Enhanced error handling
   */
  private handleError(error: HttpErrorResponse): Observable<never> {
    let errorMessage = 'An unknown error occurred!';
    
    if (error.error instanceof ErrorEvent) {
      // Client-side error
      errorMessage = `Error: ${error.error.message}`;
    } else {
      // Server-side error
      switch (error.status) {
        case 0:
          errorMessage = 'Unable to connect to server. Please check your connection.';
          break;
        case 400:
          errorMessage = 'Invalid request. Please check your input.';
          break;
        case 401:
          errorMessage = 'Invalid email or password.';
          break;
        case 403:
          errorMessage = 'Access denied. Insufficient permissions.';
          break;
        case 404:
          errorMessage = 'Authentication service not available.';
          break;
        case 500:
          errorMessage = 'Server error. Please try again later.';
          break;
        default:
          errorMessage = error.error?.message || `Server returned ${error.status}`;
      }
    }

    console.error('AuthService error:', error);
    return throwError(() => new Error(errorMessage));
  }

  /**
   * Check if user is authenticated
   */
  get isAuthenticated(): boolean {
    if (!this.isBrowser) return false;

    const token = localStorage.getItem('token');
    const tokenExpiration = localStorage.getItem('tokenExpiration');
    
    if (!token || !tokenExpiration) {
      return false;
    }

    return new Date(tokenExpiration) > new Date();
  }

  /**
   * Check if user is logged in (alias for isAuthenticated)
   */
  isLoggedIn(): boolean {
    return this.isAuthenticated;
  }

  /**
   * Get current user value
   */
  get currentUserValue(): CurrentUser | null {
    return this.currentUserSubject.value;
  }

  /**
   * Get current user as observable
   */
  get currentUser$(): Observable<CurrentUser | null> {
    return this.currentUserSubject.asObservable();
  }

  /**
   * Get authentication token
   */
  get token(): string | null {
    if (!this.isBrowser) return null;
    return localStorage.getItem('token');
  }

  /**
   * Check if user has specific role
   */
  hasRole(role: string): boolean {
    const user = this.currentUserValue;
    return user ? user.roles?.includes(role) || false : false;
  }

  /**
   * Check if user is manager/admin (for AI features)
   */
  get isManager(): boolean {
    return this.hasRole('manager') || this.hasRole('admin');
  }

  /**
   * Demo login for testing
   */
  demoLogin(): Observable<AuthResponse> {
    const demoCredentials: LoginModel = {
      email: 'demo@company.com',
      password: 'demo123'
    };

    return this.login(demoCredentials);
  }

  /**
   * Get token expiration time
   */
  getTokenExpiration(): Date | null {
    if (!this.isBrowser) return null;

    const expiration = localStorage.getItem('tokenExpiration');
    return expiration ? new Date(expiration) : null;
  }

  /**
   * Get time until token expiration (in milliseconds)
   */
  getTimeUntilExpiration(): number {
    const expiration = this.getTokenExpiration();
    if (!expiration) return 0;
    
    return expiration.getTime() - new Date().getTime();
  }

  /**
   * Manual token validation (simple check)
   */
  validateToken(): boolean {
    return this.isAuthenticated;
  }

  /**
   * Safe localStorage getter
   */
  private getLocalStorageItem(key: string): string | null {
    if (!this.isBrowser) return null;
    return localStorage.getItem(key);
  }

  /**
   * Safe localStorage setter
   */
  private setLocalStorageItem(key: string, value: string): void {
    if (!this.isBrowser) return;
    localStorage.setItem(key, value);
  }

  /**
   * Safe localStorage remover
   */
  private removeLocalStorageItem(key: string): void {
    if (!this.isBrowser) return;
    localStorage.removeItem(key);
  }
}