import { Routes } from '@angular/router';
import { TaskList } from './components/task-list/task-list';
import { TaskForm } from './components/task-form/task-form';

export const routes: Routes = [
  { path: '', component: TaskList, title: 'Tasks' },
  { path: 'new', component: TaskForm, title: 'New task' },
  { path: 'edit/:id', component: TaskForm, title: 'Edit task' },
  { path: '**', redirectTo: '' },
];
