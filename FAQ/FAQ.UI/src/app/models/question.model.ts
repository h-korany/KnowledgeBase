export interface Question {
  id: number|null;
  title: string;
  content: string;
  answers?: Answer[];
  createdAt?: Date;
  createdBy?: string;
}

export interface Answer {
  id?: number;
  questionId: number|null;
  content: string;
  createdAt?: Date;
  createdBy?: string;
}

export interface AIRequest {
  query: string;
  context: string;
}

export interface AIResponse {
  summary: string;
  relatedQuestions: number[];
}