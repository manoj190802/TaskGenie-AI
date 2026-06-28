import { CommonModule } from '@angular/common';
import { NgModule } from '@angular/core';

// Shared module for components, directives, pipes that are reused across pages
@NgModule({
  imports: [CommonModule],
  exports: [CommonModule],
})
export class SharedModule {}
