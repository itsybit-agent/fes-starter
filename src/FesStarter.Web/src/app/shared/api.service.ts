import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { CreateTodoCommand, CreateTodoResponse, ListTodosResponse } from './api.types';

@Injectable({
  providedIn: 'root'
})
export class ApiService {
  private readonly baseUrl = 'http://localhost:5000/api';

  constructor(private http: HttpClient) {}

  // CreateTodo feature
  createTodo(command: CreateTodoCommand): Observable<CreateTodoResponse> {
    return this.http.post<CreateTodoResponse>(`${this.baseUrl}/todos`, command);
  }

  // CompleteTodo feature
  completeTodo(id: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/todos/${id}/complete`, {});
  }

  // ListTodos feature
  listTodos(): Observable<ListTodosResponse> {
    return this.http.get<ListTodosResponse>(`${this.baseUrl}/todos`);
  }
}
