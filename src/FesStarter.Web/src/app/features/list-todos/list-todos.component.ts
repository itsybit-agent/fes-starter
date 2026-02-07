import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ApiService } from '../../shared/api.service';
import { TodoDto } from '../../shared/api.types';

@Component({
  selector: 'app-list-todos',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './list-todos.component.html',
  styleUrl: './list-todos.component.scss'
})
export class ListTodosComponent implements OnInit {
  todos: TodoDto[] = [];
  loading = false;
  error: string | null = null;

  constructor(private api: ApiService) {}

  ngOnInit(): void {
    this.loadTodos();
  }

  loadTodos(): void {
    this.loading = true;
    this.error = null;
    
    this.api.listTodos().subscribe({
      next: (response) => {
        this.todos = response.todos;
        this.loading = false;
      },
      error: (err) => {
        this.error = 'Failed to load todos';
        this.loading = false;
        console.error(err);
      }
    });
  }

  completeTodo(id: string): void {
    this.api.completeTodo(id).subscribe({
      next: () => {
        // Reload to get updated state
        this.loadTodos();
      },
      error: (err) => {
        this.error = 'Failed to complete todo';
        console.error(err);
      }
    });
  }
}
