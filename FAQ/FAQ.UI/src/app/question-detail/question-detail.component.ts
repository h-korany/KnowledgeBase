import { Component, OnInit, OnDestroy } from '@angular/core';
import { DatePipe } from '@angular/common';
import { ActivatedRoute, Router, NavigationEnd } from '@angular/router';
import { Question } from '../models/question.model';
import { QuestionService } from '../services/question.service';
import { AuthService } from '../services/auth.service'; // Add AuthService
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common'
import { Subject, takeUntil, filter } from 'rxjs';

// Angular Material imports
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatIconModule } from '@angular/material/icon';
import { MatTabsModule } from '@angular/material/tabs';
import { MatChipsModule } from '@angular/material/chips';
import { MatCardModule } from '@angular/material/card';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatSnackBarModule } from '@angular/material/snack-bar';
import { MatSelectModule } from '@angular/material/select';
import { MatProgressBarModule } from '@angular/material/progress-bar';

@Component({
  selector: 'app-question-detail',
  templateUrl: './question-detail.component.html',
  styleUrls: ['./question-detail.component.css'],
  standalone: true,
  imports: [
    DatePipe, 
    FormsModule, 
    CommonModule, 
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatProgressSpinnerModule,
    MatIconModule,
    MatTabsModule,
    MatChipsModule,
    MatCardModule,
    MatTooltipModule,
    MatSnackBarModule,
    MatSelectModule,
    MatProgressBarModule
  ],
})
export class QuestionDetailComponent implements OnInit, OnDestroy {
  question: Question | null = null;
  answerContent: string = '';
  loading: boolean = true;
  generatingSummary: boolean = false;
  aiSummary: string | null = null;
  error: string | null = null;
  aiError: string | null = null;
  currentDate: Date = new Date();
  isManager: boolean = false; // Add manager flag

  private destroy$ = new Subject<void>();

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private questionService: QuestionService,
    private authService: AuthService // Inject AuthService
  ) {
    this.router.events.pipe(
      takeUntil(this.destroy$),
      filter(event => event instanceof NavigationEnd)
    ).subscribe(() => {
      this.loadQuestionFromRoute();
    });
  }

  ngOnInit(): void {
    console.log('🔍 QuestionDetailComponent initialized');
    this.checkUserRole(); // Check user role on init
    this.loadQuestionFromRoute();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private checkUserRole(): void {
    // Check if user is manager
    this.isManager = this.authService.isManager;
    console.log('👤 User is manager:', this.isManager);
    
    // Optional: Subscribe to user changes if needed
    this.authService.currentUser$.pipe(
      takeUntil(this.destroy$)
    ).subscribe(user => {
      this.isManager = this.authService.isManager;
      console.log('👤 User role updated, is manager:', this.isManager);
    });
  }

  private loadQuestionFromRoute(): void {
    const questionId = this.route.snapshot.paramMap.get('id');
    console.log('🎯 Route parameter ID:', questionId);
    
    if (questionId) {
      const id = Number(questionId);
      if (!isNaN(id)) {
        this.loadQuestion(id);
      } else {
        this.error = 'Invalid question ID';
        this.loading = false;
        console.error('❌ Invalid question ID:', questionId);
      }
    } else {
      this.error = 'No question ID provided';
      this.loading = false;
      console.error('❌ No question ID in route');
    }
  }

  loadQuestion(id: number): void {
    this.loading = true;
    this.error = null;
    this.aiSummary = null;
    this.aiError = null;
    
    console.log('🔄 Loading question with ID:', id);
    
    this.questionService.getQuestion(id).subscribe({
      next: (question) => {
        this.question = question;
        this.loading = false;
        console.log('✅ Question loaded successfully:', question);
      },
      error: (error) => {
        console.error('❌ Error loading question:', error);
        this.error = 'Failed to load question';
        this.loading = false;
      }
    });
  }

  reloadQuestion(): void {
    const questionId = this.route.snapshot.paramMap.get('id');
    if (questionId) {
      this.loadQuestion(Number(questionId));
    }
  }

  generateAISummary(): void {
    // Double check authorization
    if (!this.isManager) {
      this.aiError = 'Access denied. AI features are available only for managers.';
      return;
    }

    if (!this.question) return;

    this.generatingSummary = true;
    this.aiSummary = null;
    this.aiError = null;
    this.currentDate = new Date();

    this.questionService.generateAISummary(this.question.id).subscribe({
      next: (response) => {
        this.aiSummary = response.summary;
        this.generatingSummary = false;
      },
      error: (error) => {
        this.aiError = 'Failed to generate AI summary. Please try again later.';
        this.generatingSummary = false;
        console.error('Error generating AI summary:', error);
      }
    });
  }

  submitAnswer(): void {
    if (!this.answerContent.trim() || !this.question) return;

    this.questionService.addAnswer(this.question.id, this.answerContent).subscribe({
      next: (answer) => {
        if (this.question) {
          this.question.answers?.push(answer);
        }
        this.answerContent = '';
        this.aiSummary = null;
      },
      error: (error) => {
        this.error = 'Failed to submit answer. Please try again later.';
        console.error('Error submitting answer:', error);
      }
    });
  }

  backToList(): void {
    this.router.navigate(['/']);
  }
}