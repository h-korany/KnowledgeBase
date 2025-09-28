import { Injectable } from '@angular/core';
import { Observable, of, throwError } from 'rxjs';
import { delay } from 'rxjs/operators';
import { Question, AIRequest, AIResponse } from '../models/question.model';

@Injectable({
  providedIn: 'root'
})
export class AiService {
  private readonly API_URL = 'https://localhost:44353/api/questions';
  private readonly openAiApiKey = 'your-openai-api-key'; // In a real app, this would be stored securely on the backend

  constructor() { }

  // Generate AI summary for a question
  generateSummary(question: Question): Observable<string> {
    // In a real application, this would call your backend API
    // which would then call the OpenAI API
    
    // Simulate API call with delay
    return new Observable<string>(observer => {
      // Simulate network latency
      setTimeout(() => {
        try {
          // Simulate AI-generated summary based on question content
          const summary = this.simulateAISummary(question);
          observer.next(summary);
          observer.complete();
        } catch (error) {
          observer.error('Failed to generate AI summary');
        }
      }, 2000); // Simulate 2-second delay
    });
  }

  // Ask AI assistant a question
  askAI(query: string, contextQuestions: Question[] = []): Observable<{response: string, relatedQuestions: number[]}> {
    // In a real application, this would call your backend API
    // which would then call the OpenAI API
    
    // Simulate API call with delay
    return new Observable<{response: string, relatedQuestions: number[]}>(observer => {
      // Simulate network latency
      setTimeout(() => {
        try {
          // Simulate AI response based on query
          const response = this.simulateAIResponse(query, contextQuestions);
          observer.next(response);
          observer.complete();
        } catch (error) {
          observer.error('Failed to get AI response');
        }
      }, 1500); // Simulate 1.5-second delay
    });
  }

  // Simulate AI summary generation (would be replaced with actual API call)
  private simulateAISummary(question: Question): string {
    const summaries: {[key: string]: string} = {
      'How to reset password?': 'Based on the answers provided, the main solution for password reset is to either contact IT support at ext. 5555 or use the "Forgot Password" link on the login page. The second option requires answering security questions. Several employees have reported success with the self-service option.',
      'VPN connection issues': 'The answers suggest ensuring you have the latest VPN client version, checking your network connection, and verifying your authentication credentials. If issues persist, contacting network support is recommended.',
      'Expense report submission': 'Currently, there are no answers to this question. The AI suggests looking at the company policy documents or contacting the finance department directly for guidance on expense report submission.',
      'Onboarding process for new hires': 'Based on the provided answer, new hires complete paperwork on day one, orientation on day two, and department training on day three. For detailed document requirements, consult the HR portal or contact HR directly.'
    };

    return summaries[question.title] || 'AI summary is not available for this question at the moment.';
  }

  // Simulate AI response (would be replaced with actual API call)
  private simulateAIResponse(query: string, contextQuestions: Question[]): {response: string, relatedQuestions: number[]} {
    const queryLower = query.toLowerCase();
    let response = '';
    let relatedQuestions: number[] = [];

    if (queryLower.includes('password') || queryLower.includes('login')) {
      response = 'Based on the knowledge base, password issues are commonly resolved by:\n\n1. Contacting IT support at ext. 5555\n2. Using the "Forgot Password" feature on the login page\n3. Answering security questions for verification\n\nPassword reset typically takes 15-30 minutes during business hours.';
      relatedQuestions = [1];
    } else if (queryLower.includes('vpn') || queryLower.includes('remote')) {
      response = 'VPN connection issues are often related to:\n\n1. Outdated VPN client software\n2. Network firewall restrictions\n3. Incorrect authentication credentials\n4. Company network outages\n\nEnsure you have the latest VPN client and check the IT status portal for any ongoing outages.';
      relatedQuestions = [2];
    } else if (queryLower.includes('onboarding') || queryLower.includes('new hire')) {
      response = 'The onboarding process for new hires includes:\n\n1. Day 1: Paperwork completion and system setup\n2. Day 2: Company orientation and policy review\n3. Day 3: Department-specific training\n4. Week 1: Shadowing and introductory meetings\n\nRequired documents typically include ID, tax forms, and direct deposit information.';
      relatedQuestions = [4];
    } else if (queryLower.includes('expense') || queryLower.includes('report')) {
      response = 'Expense report guidelines:\n\n1. Submit within 30 days of expenditure\n2. Use the company template available on the finance portal\n3. Include all receipts above $25\n4. Manager approval required before submission\n5. Processing takes 7-10 business days\n\nFor specific questions, contact finance@company.com.';
      relatedQuestions = [3];
    } else {
      response = 'I found some general information in our knowledge base. The most common topics employees ask about include:\n\n1. IT issues (password resets, VPN access)\n2. HR processes (onboarding, benefits)\n3. Finance procedures (expense reports, reimbursements)\n4. Operational guidelines\n\nCould you please clarify your question or specify which area you need help with?';
      relatedQuestions = [1, 2, 3, 4];
    }

    return { response, relatedQuestions };
  }

  // In a real application, this would call your backend API
  private callOpenAIAPI(prompt: string): Observable<string> {
    // This is a placeholder for the actual API call
    // The actual implementation would use HttpClient to call your backend
    // which would then call the OpenAI API with the secure API key
    
    console.log('Simulating API call to OpenAI with prompt:', prompt);
    return of('AI response based on: ' + prompt).pipe(delay(1000));
  }
}