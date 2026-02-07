import { Component, EventEmitter, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../shared/api.service';

@Component({
  selector: 'app-create-todo',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './create-todo.component.html',
  styleUrl: './create-todo.component.scss'
})
export class CreateTodoComponent {
  title = '';
  submitting = false;
  error: string | null = null;

  @Output() todoCreated = new EventEmitter<void>();

  constructor(private api: ApiService) {}

  submit(): void {
    if (!this.title.trim()) {
      this.error = 'Title is required';
      return;
    }

    this.submitting = true;
    this.error = null;

    this.api.createTodo({ title: this.title }).subscribe({
      next: () => {
        this.title = '';
        this.submitting = false;
        this.todoCreated.emit();
      },
      error: (err) => {
        this.error = 'Failed to create todo';
        this.submitting = false;
        console.error(err);
      }
    });
  }
}
