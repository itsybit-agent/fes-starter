import { Component, OnInit, signal } from '@angular/core';
import { ApiService } from '../../shared/api.service';
import { TodoDto } from '../../shared/api.types';

@Component({
  selector: 'app-list-todos',
  standalone: true,
  templateUrl: './list-todos.component.html',
  styleUrl: './list-todos.component.scss'
})
export class ListTodosComponent implements OnInit {
  todos = signal<TodoDto[]>([]);
  loading = signal(false);
  error = signal<string | null>(null);

  constructor(private api: ApiService) {}

  ngOnInit(): void {
    this.loadTodos();
  }

  loadTodos(): void {
    this.loading.set(true);
    this.error.set(null);

    this.api.listTodos().subscribe({
      next: (response) => {
        this.todos.set(response.todos);
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set('Failed to load todos');
        this.loading.set(false);
        console.error(err);
      }
    });
  }

  completeTodo(id: string): void {
    this.api.completeTodo(id).subscribe({
      next: () => {
        this.loadTodos();
      },
      error: (err) => {
        this.error.set('Failed to complete todo');
        console.error(err);
      }
    });
  }
}
