import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { QuestionService } from '../services/question.service';
import {FormsModule} from '@angular/forms';

@Component({
  selector: 'app-ask-question',
  templateUrl: './ask-question.component.html',
  styleUrls: ['./ask-question.component.css'],
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, FormsModule]
})
export class AskQuestionComponent implements OnInit {
  questionForm: FormGroup;
  loading = false;
  error: string | null = null;
  tagInput = '';

  constructor(
    private fb: FormBuilder,
    private questionService: QuestionService,
    private router: Router
  ) {
    this.questionForm = this.fb.group({
      title: ['', [Validators.required, Validators.minLength(10)]],
      content: ['', [Validators.required, Validators.minLength(20)]],
      tags: [[]]
    });
  }

  ngOnInit(): void {
    // Component initialization if needed
  }

  // Add a tag to the question
  addTag(event: any): void {
    if (event.key === 'Enter' && this.tagInput.trim()) {
      event.preventDefault();
      const currentTags = this.questionForm.get('tags')?.value || [];
      const newTag = this.tagInput.trim();
      
      // Avoid duplicate tags
      if (!currentTags.includes(newTag)) {
        this.questionForm.get('tags')?.setValue([...currentTags, newTag]);
      }
      
      this.tagInput = '';
    }
  }

  // Remove a tag from the question
  removeTag(tag: string): void {
    const currentTags = this.questionForm.get('tags')?.value || [];
    const updatedTags = currentTags.filter((t: string) => t !== tag);
    this.questionForm.get('tags')?.setValue(updatedTags);
  }

  // Submit the question form
  submitQuestion(): void {
    if (this.questionForm.invalid) {
      this.markFormGroupTouched();
      return;
    }

    this.loading = true;
    this.error = null;

    const questionData = {
      title: this.questionForm.get('title')?.value,
      content: this.questionForm.get('content')?.value,
      id:null
    };

    this.questionService.addQuestion(questionData).subscribe({
      next: (question) => {
        this.loading = false;
        this.router.navigate(['/question', question.id]);
      },
      error: (error) => {
        this.error = 'Failed to submit question. Please try again later.';
        this.loading = false;
        console.error('Error submitting question:', error);
      }
    });
  }

  // Cancel and go back to the questions list
  cancel(): void {
    this.router.navigate(['/']);
  }

  // Mark all form fields as touched to show validation errors
  private markFormGroupTouched(): void {
    Object.keys(this.questionForm.controls).forEach(key => {
      const control = this.questionForm.get(key);
      control?.markAsTouched();
    });
  }

  // Get validation error message for title field
  getTitleErrorMessage(): string {
    const titleControl = this.questionForm.get('title');
    
    if (titleControl?.hasError('required')) {
      return 'Title is required';
    }
    
    if (titleControl?.hasError('minlength')) {
      return 'Title must be at least 10 characters long';
    }
    
    return '';
  }

  // Get validation error message for content field
  getContentErrorMessage(): string {
    const contentControl = this.questionForm.get('content');
    
    if (contentControl?.hasError('required')) {
      return 'Question content is required';
    }
    
    if (contentControl?.hasError('minlength')) {
      return 'Question content must be at least 20 characters long';
    }
    
    return '';
  }

  // Check if title field has error
  hasTitleError(): boolean {
    const titleControl = this.questionForm.get('title');
    return !!titleControl && titleControl.invalid && (titleControl.dirty || titleControl.touched);
  }

  // Check if content field has error
  hasContentError(): boolean {
    const contentControl = this.questionForm.get('content');
    return !!contentControl && contentControl.invalid && (contentControl.dirty || contentControl.touched);
  }
}