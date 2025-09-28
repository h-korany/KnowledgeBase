import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface SummaryRequest {
  questionTitle: string;
  questionContent: string;
  answers: string[]| undefined;
}

export interface SummaryResponse {
  summary: string;
}

@Injectable({
  providedIn: 'root'
})
export class OpenAIService {
  private apiUrl = 'https://localhost:44353/api/questions';

  constructor(private http: HttpClient) { }

  generateSummary(request: SummaryRequest): Observable<SummaryResponse> {
    return this.http.post<SummaryResponse>(`${this.apiUrl}/summary`, request);
  }
  
}