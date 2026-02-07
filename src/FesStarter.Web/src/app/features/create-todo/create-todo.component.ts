import { Component, EventEmitter, Output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../shared/api.service';

@Component({
  selector: 'app-create-todo',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './create-todo.component.html',
  styleUrl: './create-todo.component.scss'
})
export class CreateTodoComponent {
  title = signal('');
  submitting = signal(false);
  error = signal<string | null>(null);

  @Output() todoCreated = new EventEmitter<void>();

  constructor(private api: ApiService) {}

  submit(): void {
    if (!this.title().trim()) {
      this.error.set('Title is required');
      return;
    }

    this.submitting.set(true);
    this.error.set(null);

    this.api.createTodo({ title: this.title() }).subscribe({
      next: () => {
        this.title.set('');
        this.submitting.set(false);
        this.todoCreated.emit();
      },
      error: (err) => {
        this.error.set('Failed to create todo');
        this.submitting.set(false);
        console.error(err);
      }
    });
  }
}
