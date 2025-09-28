import { Routes } from '@angular/router';
import { QuestionListComponent } from './question-list/question-list.component';
import { QuestionDetailComponent } from './question-detail/question-detail.component';
import { AskQuestionComponent } from './ask-question/ask-question.component';
import { LoginComponent } from './auth/login/login.component';
import { AuthGuard } from './guards/auth.guard';
import { RegisterComponent } from './auth/register/register.component';

export const routes: Routes = [
    { path: 'login', component: LoginComponent },
  { path: 'register', component: RegisterComponent },
  { path: 'questionList', component: QuestionListComponent , canActivate: [AuthGuard] },
  { path: 'question/:id', component: QuestionDetailComponent, canActivate: [AuthGuard],runGuardsAndResolvers: 'always'  },
  { path: 'ask', component: AskQuestionComponent, canActivate: [AuthGuard] },
  { path: '', component: QuestionListComponent , canActivate: [AuthGuard] },
  { path: '**', redirectTo: '' }
];