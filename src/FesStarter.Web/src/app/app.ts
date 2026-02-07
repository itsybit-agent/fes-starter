import { Component, ViewChild } from '@angular/core';
import { CreateTodoComponent } from './features/create-todo/create-todo.component';
import { ListTodosComponent } from './features/list-todos/list-todos.component';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CreateTodoComponent, ListTodosComponent],
  templateUrl: './app.html',
  styleUrl: './app.scss'
})
export class App {
  @ViewChild(ListTodosComponent) listTodos!: ListTodosComponent;

  onTodoCreated(): void {
    this.listTodos.loadTodos();
  }
}
