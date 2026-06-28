import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { AssignmentsComponent } from './assignments.component';

@NgModule({
  declarations: [AssignmentsComponent],
  imports: [
    CommonModule, FormsModule,
    RouterModule.forChild([{ path: '', component: AssignmentsComponent }]),
  ],
})
export class AssignmentsModule {}
