// app.component.ts
import { Component, OnInit, OnDestroy, Inject, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { Router, RouterOutlet, RouterModule, NavigationEnd } from '@angular/router'; // Import RouterOutlet
import { AiAssistantComponent } from "./ai-assistant/ai-assistant.component";
import { CommonModule } from "@angular/common";
import { MatDialog } from '@angular/material/dialog';
import { AuthService } from './services/auth.service';
import { CurrentUser } from './models/user.model';
import { Subject, takeUntil, filter } from 'rxjs';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [
    RouterOutlet, // Use RouterOutlet instead of RouterModule
    AiAssistantComponent, 
    CommonModule, 
    RouterModule // Keep RouterModule for routerLink if needed
  ],
  templateUrl: './app.component.html',
  styleUrl: './app.component.css'
})
export class AppComponent implements OnInit, OnDestroy {
  title = 'Knowledge Base';
  showAIAssistant = false;
  isManager = false;
  currentUser: CurrentUser | null = null;
  isBrowser: boolean;
  showNavigation = true;

  private destroy$ = new Subject<void>();

  constructor(
    @Inject(PLATFORM_ID) private platformId: any,
    private dialog: MatDialog,
    private authService: AuthService,
    private router: Router
  ) {
    this.isBrowser = isPlatformBrowser(this.platformId);
  }

  ngOnInit(): void {
    // Subscribe to authentication state changes
    if (this.isBrowser) {
      this.authService.currentUser$
        .pipe(takeUntil(this.destroy$))
        .subscribe(user => {
          this.currentUser = user;
          this.isManager = this.authService.isManager;
        });

      // Listen to route changes to show/hide navigation
      this.router.events
        .pipe(
          takeUntil(this.destroy$),
          filter(event => event instanceof NavigationEnd)
        )
        .subscribe((event: any) => {
          this.updateNavigationVisibility(event.url);
        });

      // Initial check
      this.updateNavigationVisibility(this.router.url);
    }
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  /**
   * Update navigation visibility based on current route
   */
  private updateNavigationVisibility(url: string): void {
    // Hide navigation on login and register pages
    this.showNavigation = !url.includes('/login') && !url.includes('/register');
    console.log('Navigation visibility:', this.showNavigation, 'for URL:', url);
  }

  openAIAssistant(): void {
    if (!this.isBrowser || !this.isManager) {
      console.warn('⚠️ Non-manager user attempted to access AI Assistant');
      return;
    }

    const dialogRef = this.dialog.open(AiAssistantComponent, {
      width: '90vw',
      maxWidth: '1200px',
      height: '85vh',
      maxHeight: '700px',
      panelClass: 'ai-assistant-dialog',
      autoFocus: false,
      disableClose: false
    });

    dialogRef.afterClosed().subscribe(result => {
      console.log('AI Assistant dialog was closed');
    });
  }

  logout(): void {
    this.authService.logout();
  }

  closeAIAssistant(): void {
    this.showAIAssistant = false;
  }
}