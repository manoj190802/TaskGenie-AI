import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { DevelopersComponent } from './developers.component';

@NgModule({
  declarations: [DevelopersComponent],
  imports: [
    CommonModule, FormsModule,
    RouterModule.forChild([{ path: '', component: DevelopersComponent }]),
  ],
})
export class DevelopersModule {}
