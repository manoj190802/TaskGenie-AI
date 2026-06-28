import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { TasksComponent } from './tasks.component';
import { TaskDetailComponent } from './task-detail/task-detail.component';

@NgModule({
  declarations: [TasksComponent, TaskDetailComponent],
  imports: [
    CommonModule, FormsModule,
    RouterModule.forChild([
      { path: '', component: TasksComponent },
      { path: ':id', component: TaskDetailComponent },
    ]),
  ],
})
export class TasksModule {}
