import { Component, EventEmitter, Output, OnInit, OnDestroy } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { AIAssistantService, AIAssistantRequest, AIAssistantResponse, KnowledgeBaseAnalysis, CategoryStats, QuestionActivity } from './../services/ai-assistant.service';
import { QuestionService } from './../services/question.service';
import { Question } from './../models/question.model';

import { Subject } from 'rxjs';
import { takeUntil, debounceTime, distinctUntilChanged } from 'rxjs/operators';

// Remove MatDialogRef import
import { MatSnackBar } from '@angular/material/snack-bar';

// Keep other Angular Material imports but remove MatDialogModule
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
import { ReactiveFormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { MatLine, MatLineModule } from '@angular/material/core';
import { MatDialogRef } from '@angular/material/dialog';

@Component({
  selector: 'app-ai-assistant',
  templateUrl: './ai-assistant.component.html',
  styleUrls: ['./ai-assistant.component.css'],
  standalone: true,
  imports: [
    // Remove MatDialogModule from imports
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
    MatProgressBarModule,
    ReactiveFormsModule,
    CommonModule,
    MatLineModule,
  ]
})
export class AiAssistantComponent implements OnInit, OnDestroy {
  @Output() close = new EventEmitter<void>();
  
  // Form
  aiForm: FormGroup;
  
  // Loading states
  loading = false;
  analysisLoading = false;
  categoriesLoading = false;
  searchLoading = false;
  
  // Data states
  error: string | null = null;
  response: AIAssistantResponse | null = null;
  analysis: KnowledgeBaseAnalysis | null = null;
  availableCategories: string[] = [];
  recentQuestions: Question[] = [];
  relatedQuestions: Question[] = [];
  searchResults: Question[] = [];
  
  // UI state
  activeTab: 'chat' | 'analysis' = 'chat';
  searchQuery = '';
  
  private destroy$ = new Subject<void>();

  constructor(
    private fb: FormBuilder,
    private aiAssistantService: AIAssistantService,
    private questionService: QuestionService,
    private router: Router,
    // Remove MatDialogRef from constructor
    private snackBar: MatSnackBar,
    public dialogRef: MatDialogRef<AiAssistantComponent>
  ) {
    // Initialize reactive form with validation
    this.aiForm = this.fb.group({
      query: ['', [
        Validators.required, 
        Validators.minLength(3), 
        Validators.maxLength(500)
      ]],
      category: ['']
    });

    // Setup search debouncing
    //this.setupSearch();
  }

  // ... keep all your existing methods but update the navigation methods:

  closeDialog(): void {
    this.dialogRef.close();
  }

  viewQuestion(questionId: number | null): void {
  if (!questionId) {
    console.error('Question ID is null or undefined');
    return;
  }
  
  console.log('Navigating to question:', questionId); // Debug log
  
  // Close the dialog first
  this.dialogRef.close();
  
  // Navigate to the question details page
  this.router.navigate(['/question', questionId]); // Note: using 'questions' plural
}


  ngOnInit(): void {
    this.loadInitialData();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  // Setup real-time search with debouncing
  /* private setupSearch(): void {
    this.aiForm.get('query')?.valueChanges
      .pipe(
        debounceTime(300),
        distinctUntilChanged(),
        takeUntil(this.destroy$)
      )
      .subscribe(query => {
        this.searchQuery = query;
        if (query.length >= 2) {
          this.searchQuestions(query);
        } else {
          this.searchResults = [];
        }
      });
  } */

  // Load all initial data from APIs
  private loadInitialData(): void {
    this.loadRecentQuestions();
    this.loadCategories();
    this.loadAnalysis();
  }

  // Load recent questions from API
  private loadRecentQuestions(): void {
    this.questionService.getQuestions().subscribe({
      next: (questions) => {
        this.recentQuestions = questions.slice(0, 6);
      },
      error: (error) => {
        console.error('Error loading recent questions:', error);
        this.showError('Failed to load recent questions');
      }
    });
  }

  // Load categories from API
  private loadCategories(): void {
    this.categoriesLoading = true;
    this.aiAssistantService.getCategories().subscribe({
      next: (categories) => {
        this.availableCategories = categories;
        this.categoriesLoading = false;
      },
      error: (error) => {
        console.error('Error loading categories:', error);
        this.availableCategories = ['IT', 'HR', 'Finance', 'Operations', 'Onboarding'];
        this.categoriesLoading = false;
        this.showError('Failed to load categories');
      }
    });
  }

  // Load knowledge base analysis from API
  private loadAnalysis(): void {
    this.analysisLoading = true;
    this.aiAssistantService.analyzeKnowledgeBase().subscribe({
      next: (analysis) => {
        this.analysis = analysis;
        this.analysisLoading = false;
      },
      error: (error) => {
        console.error('Error loading analysis:', error);
        this.analysisLoading = false;
        this.showError('Failed to load analysis data');
      }
    });
  }

  // Search questions from API
  /* private searchQuestions(query: string): void {
    this.searchLoading = true;
    this.aiAssistantService.searchQuestions(query).subscribe({
      next: (results) => {
        this.searchResults = results.slice(0, 5);
        this.searchLoading = false;
      },
      error: (error) => {
        console.error('Error searching questions:', error);
        this.searchResults = [];
        this.searchLoading = false;
      }
    });
  } */

  // Main AI question submission
  askAI(): void {
    if (this.aiForm.invalid) {
      this.markFormGroupTouched();
      return;
    }

    this.loading = true;
    this.error = null;
    this.response = null;
    this.relatedQuestions = [];

    const request: AIAssistantRequest = {
      query: this.aiForm.get('query')?.value.trim(),
      category: this.aiForm.get('category')?.value || undefined
    };

    this.aiAssistantService.askQuestion(request).subscribe({
      next: (response) => {
        this.response = response;
        this.loadRelatedQuestions(response.relatedQuestionIds);
        this.loading = false;
        this.showSuccess('AI response received successfully');
      },
      error: (error) => {
        this.error = error.message;
        this.loading = false;
        this.showError('Failed to get AI response');
      }
    });
  }

  // Load related questions for AI response
  private loadRelatedQuestions(questionIds: number[]): void {
    if (!questionIds.length) return;

    const loadedQuestions: Question[] = [];
    const loadPromises = questionIds.map(id => 
      this.questionService.getQuestion(id).toPromise()
    );

    Promise.allSettled(loadPromises).then(results => {
      results.forEach(result => {
        if (result.status === 'fulfilled' && result.value) {
          loadedQuestions.push(result.value);
        }
      });
      this.relatedQuestions = loadedQuestions;
    });
  }

  // Get category stats for visualization
  getCategoryStats(): CategoryStats[] {
    if (!this.analysis?.categoryStats) return [];

    const total = this.analysis.totalQuestions || 1;
    return Object.entries(this.analysis.categoryStats)
      .map(([name, count]) => ({
        name,
        count,
        percentage: Math.round((count / total) * 100)
      }))
      .sort((a, b) => b.count - a.count)
      .slice(0, 8);
  }

  // Analyze by specific category
  analyzeByCategory(category: string): void {
    this.analysisLoading = true;
    this.aiAssistantService.analyzeKnowledgeBase(category).subscribe({
      next: (analysis) => {
        this.analysis = analysis;
        this.analysisLoading = false;
        this.showSuccess(`Analysis updated for ${category}`);
      },
      error: (error) => {
        this.analysisLoading = false;
        this.showError('Failed to load category analysis');
      }
    });
  }

  // Refresh all data
  refreshData(): void {
    this.loadAnalysis();
    this.loadCategories();
    this.loadRecentQuestions();
  }

  

  useSearchResult(question: Question): void {
    this.aiForm.patchValue({
      query: `Tell me more about: ${question.title}`
    });
    this.searchResults = [];
  }

  // UI interaction methods
  setActiveTab(tab: 'chat' | 'analysis'): void {
    this.activeTab = tab;
    if (tab === 'analysis' && !this.analysis) {
      this.loadAnalysis();
    }
  }

  

  resetForm(): void {
    this.aiForm.reset();
    this.response = null;
    this.relatedQuestions = [];
    this.error = null;
    this.searchResults = [];
  }

  // Validation helpers
  private markFormGroupTouched(): void {
    Object.keys(this.aiForm.controls).forEach(key => {
      this.aiForm.get(key)?.markAsTouched();
    });
  }

  getQueryError(): string {
    const control = this.aiForm.get('query');
    if (control?.hasError('required')) return 'Question is required';
    if (control?.hasError('minlength')) return 'Minimum 3 characters required';
    if (control?.hasError('maxlength')) return 'Maximum 500 characters allowed';
    return '';
  }

  isFieldInvalid(fieldName: string): boolean {
    const control = this.aiForm.get(fieldName);
    return !!control && control.invalid && (control.dirty || control.touched);
  }

  // Notification helpers
  private showError(message: string): void {
    this.snackBar.open(message, 'Close', {
      duration: 5000,
      panelClass: ['error-snackbar']
    });
  }

  private showSuccess(message: string): void {
    this.snackBar.open(message, 'Close', {
      duration: 3000,
      panelClass: ['success-snackbar']
    });
  }

  // Getters for template
  get hasSearchResults(): boolean {
    return this.searchResults.length > 0 && this.searchQuery.length >= 2;
  }

  get hasAnalysisData(): boolean {
    return !!this.analysis && !this.analysisLoading;
  }

  get canSubmit(): boolean {
    return this.aiForm.valid && !this.loading;
  }
}