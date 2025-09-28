import { Injectable } from '@angular/core';
import { Observable, of, throwError } from 'rxjs';
import { delay, map, tap } from 'rxjs/operators';
import { Question, Answer } from '../models/question.model';
import { HttpClient } from '@angular/common/http';
import { OpenAIService, SummaryRequest } from './openai.service';

@Injectable({
  providedIn: 'root'
})
export class QuestionService {
  private questions: Question[] =[];
  /*= [
    {
      id: 1,
      title: 'How to reset password?',
      content: 'I forgot my password to the internal system and need to reset it. What\'s the procedure for resetting my password?',
      tags: ['IT', 'Password', 'Login'],
      answers: [
        {
          id: 1,
          questionId: 1,
          content: 'You need to contact IT support at ext. 5555 or email it-help@company.com. They will send you a temporary password.',
          createdAt: new Date(Date.now() - 86400000), // 1 day ago
          createdBy: 'Jane Smith'
        },
        {
          id: 2,
          questionId: 1,
          content: 'Alternatively, you can use the "Forgot Password" link on the login page. You\'ll need to answer your security questions.',
          createdAt: new Date(Date.now() - 43200000), // 12 hours ago
          createdBy: 'IT Support'
        }
      ],
      createdAt: new Date(Date.now() - 172800000), // 2 days ago
      createdBy: 'John Doe'
    },
    {
      id: 2,
      title: 'VPN connection issues',
      content: 'I can\'t connect to the corporate VPN from home. I\'ve followed the setup instructions but keep getting an authentication error. What should I do?',
      tags: ['IT', 'VPN', 'Remote'],
      answers: [
        {
          id: 3,
          questionId: 2,
          content: 'Make sure you\'re using the latest version of the VPN client. You can download it from the IT portal.',
          createdAt: new Date(Date.now() - 172800000), // 2 days ago
          createdBy: 'Network Admin'
        }
      ],
      createdAt: new Date(Date.now() - 259200000), // 3 days ago
      createdBy: 'Sarah Johnson'
    },
    {
      id: 3,
      title: 'Expense report submission',
      content: 'What\'s the process for submitting expense reports? Is there a specific format or template we need to use?',
      tags: ['Finance', 'Expenses'],
      answers: [],
      createdAt: new Date(Date.now() - 86400000), // 1 day ago
      createdBy: 'Mike Thompson'
    },
    {
      id: 4,
      title: 'Onboarding process for new hires',
      content: 'What is the complete onboarding process for new employees? What documents are required and what training sessions are mandatory?',
      tags: ['HR', 'Onboarding'],
      answers: [
        {
          id: 4,
          questionId: 4,
          content: 'New hires need to complete paperwork on day one, attend orientation on day two, and department-specific training on day three.',
          createdAt: new Date(Date.now() - 345600000), // 4 days ago
          createdBy: 'HR Manager'
        }
      ],
      createdAt: new Date(Date.now() - 432000000), // 5 days ago
      createdBy: 'New Employee'
    }
  ];*/

  private lastQuestionId = 4;
  private lastAnswerId = 4;

  private readonly API_URL = 'https://localhost:44353/api/questions';
  constructor(private http: HttpClient,private openAIService: OpenAIService) { }

  generateAISummary(questionId: number|null): Observable<{summary: string}> {
    // First get the question and answers
    return new Observable(observer => {
      this.getQuestion(questionId).subscribe({
        next: (question) => {
          const answersContent = question.answers?.map(answer => answer.content);
          
          const summaryRequest: SummaryRequest = {
            questionTitle: question.title,
            questionContent: question.content,
            answers: answersContent
          };

          // Call OpenAI service
          this.openAIService.generateSummary(summaryRequest).subscribe({
            next: (response) => {
              observer.next(response);
              observer.complete();
            },
            error: (error) => {
              observer.error(error);
            }
          });
        },
        error: (error) => {
          observer.error(error);
        }
      });
    });
  }

  // Get all questions
  getQuestions(): Observable<Question[]> {
    // Simulate API call with delay
    return this.http.get<Question[]>(this.API_URL).pipe(
      // The `tap` operator lets you perform side effects (like setting a variable)
      // without affecting the observable stream.
      tap(q => {
        this.questions = q;
        console.log('Questions stored in service:', this.questions);
      })
    );;
  }

  // Get a specific question by ID
  getQuestion(id: number|null): Observable<Question> {
    /* const question = this.questions.find(q => q.id === id);
    
    if (question) {
      return of({...question}).pipe(
        delay(300) // Simulate network latency
      );
    } else {
      return throwError(() => new Error(`Question with ID ${id} not found`));
    } */

    return this.http.get<Question>(`${this.API_URL}/${id}`);
  }

  // Add a new question
  addQuestion(question: { title: string; content: string; }): Observable<Question> {
    const newQuestion: Question = {
      title: question.title,
      content: question.content,
      id: null
    };

    return this.http.post<Question>(this.API_URL, newQuestion);
  }

  // Add an answer to a question
  addAnswer(questionId: number|null, content: string): Observable<Answer> {
    /* const question = this.questions.find(q => q.id === questionId);
    
    if (!question) {
      return throwError(() => new Error(`Question with ID ${questionId} not found`));
    }

    const newAnswer: Answer = {
      questionId,
      content
    };

    question.answers?.push(newAnswer); */

    const newAnswer: Answer = {
      questionId,
      content
    };
    
    return this.http.post<Answer>(this.API_URL+"/"+questionId, newAnswer);
  }

  // Search questions by query
  searchQuestions(query: string): Observable<Question[]> {
    const normalizedQuery = query.toLowerCase().trim();
    
    if (!normalizedQuery) {
      return this.getQuestions();
    }

    const filteredQuestions = this.questions.filter(question => 
      question.title.toLowerCase().includes(normalizedQuery) ||
      question.content.toLowerCase().includes(normalizedQuery) );

    return of(filteredQuestions).pipe(
      delay(400) // Simulate network latency
    );
  }

  // Get questions by tag
  
}