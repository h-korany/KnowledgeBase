import { Injectable } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Observable, throwError, of } from 'rxjs';
import { catchError, map, retry, timeout } from 'rxjs/operators';
import { TimeoutError } from 'rxjs';

export interface AIAssistantRequest {
  query: string;
  category?: string;
}

export interface AIAssistantResponse {
  response: string;
  relatedQuestionIds: number[];
  source: string;
  confidence?: number;
  timestamp?: string;
}

export interface KnowledgeBaseAnalysis {
  totalQuestions: number;
  answeredQuestions: number;
  unansweredQuestions: number;
  answerRate: number;
  averageAnswersPerQuestion: number;
  popularCategories: string[];
  categoryStats: { [key: string]: number };
  recentActivity: QuestionActivity[];
  generatedAt?: string;
}

export interface QuestionActivity {
  period: string;
  questionsCount: number;
  answersCount: number;
}

export interface CategoryStats {
  name: string;
  count: number;
  percentage: number;
}

@Injectable({
  providedIn: 'root'
})
export class AIAssistantService {
  private apiUrl = 'https://localhost:44353/api/questions';

  constructor(private http: HttpClient) { }

  // Ask AI question with proper typing
  askQuestion(request: AIAssistantRequest): Observable<AIAssistantResponse> {
    return this.http.post<AIAssistantResponse>(`${this.apiUrl}/ask`, request).pipe(
      timeout(30000),
      retry(2),
      catchError((error: HttpErrorResponse) => this.handleError<AIAssistantResponse>('askQuestion', error))
    );
  }

  // Get knowledge base analysis
  analyzeKnowledgeBase(category?: string): Observable<KnowledgeBaseAnalysis> {
    const params: any = {};
    if (category) {
      params.category = category;
    }

    return this.http.get<KnowledgeBaseAnalysis>(`${this.apiUrl}/analyze`).pipe(
      timeout(15000),
      catchError((error: HttpErrorResponse) => this.handleError<KnowledgeBaseAnalysis>('analyzeKnowledgeBase', error))
    );
  }

  // Get available categories
 getCategories(): Observable<string[]> {
  return this.http.get<string[]>(`${this.apiUrl}/categories`).pipe(
    timeout(30000), // 30 seconds timeout
    catchError((error: any) => {
      console.error('getCategories API error:', error);
      
      // Check if it's a TimeoutError
      if (error instanceof TimeoutError) {
        throw new Error('Request timeout: Categories service is not responding');
      }
      
      // Return fallback categories for other errors
      const fallbackCategories = ['IT', 'HR', 'Finance', 'Operations', 'Onboarding'];
      return of(fallbackCategories);
    }),
    retry(2)
  );
}

  // Get quick stats
  getQuickStats(): Observable<{total: number, answered: number, rate: number}> {
    return this.http.get<{total: number, answered: number, rate: number}>(`${this.apiUrl}/stats`).pipe(
      timeout(10000),
      catchError((error: HttpErrorResponse) => this.handleError('getQuickStats', error, {total: 0, answered: 0, rate: 0}))
    );
  }

  // Get unanswered questions
  /* getUnansweredQuestions(): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/unanswered`).pipe(
      timeout(10000),
      catchError((error: HttpErrorResponse) => this.handleError<any[]>('getUnansweredQuestions', error, []))
    );
  }

  // Search questions
  searchQuestions(query: string): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/search`, {
      params: { q: query }
    }).pipe(
      timeout(10000),
      catchError((error: HttpErrorResponse) => this.handleError<any[]>('searchQuestions', error, []))
    );
  } */

  // Generic error handler with proper typing
  private handleError<T>(operation: string, error: HttpErrorResponse, result?: T): Observable<T> {
    console.error(`${operation} failed:`, error);
    
    let userFriendlyMessage: string;
    
    if (error.error instanceof ErrorEvent) {
      userFriendlyMessage = 'Network error occurred. Please check your connection.';
    } else {
      switch (error.status) {
        case 0:
          userFriendlyMessage = 'Cannot connect to server. Please try again later.';
          break;
        case 404:
          userFriendlyMessage = 'Requested resource not found.';
          break;
        case 500:
          userFriendlyMessage = 'Server error occurred. Please try again later.';
          break;
        case 503:
          userFriendlyMessage = 'Service temporarily unavailable. Please try again later.';
          break;
        default:
          userFriendlyMessage = 'An unexpected error occurred. Please try again.';
      }
    }
    
    // Return fallback value if provided, otherwise throw error
    if (result !== undefined) {
      return of(result as T);
    }
    
    return throwError(() => new Error(userFriendlyMessage));
  }
}