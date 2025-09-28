import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { Question } from '../models/question.model';
import { QuestionService } from '../services/question.service';
import { TruncatePipe } from '../truncate.pipe';

import {FormsModule} from '@angular/forms';
import {CommonModule} from '@angular/common'
@Component({
  selector: 'app-question-list',
  templateUrl: './question-list.component.html',
  styleUrls: ['./question-list.component.css'],
    standalone: true, // Mark as standalone component
    imports: [ TruncatePipe,FormsModule, CommonModule] 

})
export class QuestionListComponent implements OnInit {
  questions: Question[] = [];
  filteredQuestions: Question[] = [];
  searchQuery: string = '';
  filterCategory: string = '';
  loading: boolean = true;
  error: string | null = null;

  constructor(
    private questionService: QuestionService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.loadQuestions();
  }

  loadQuestions(): void {
    this.loading = true;
    this.questionService.getQuestions().subscribe({
      next: (questions) => {
        this.questions = questions;
        this.filteredQuestions = questions;
        this.loading = false;
      },
      error: (error) => {
        this.error = 'Failed to load questions. Please try again later.';
        this.loading = false;
        console.error('Error loading questions:', error);
      }
    });
  }

  filterQuestions(): void {
    this.filteredQuestions = this.questions.filter(question => {
      const matchesSearch = question.title.toLowerCase().includes(this.searchQuery.toLowerCase()) ||
                           question.content.toLowerCase().includes(this.searchQuery.toLowerCase());
      const matchesCategory = !this.filterCategory ;
      return matchesSearch && matchesCategory;
    });
  }

  viewQuestion(questionId: number|null): void {
    this.router.navigate(['/question', questionId]);
  }
}