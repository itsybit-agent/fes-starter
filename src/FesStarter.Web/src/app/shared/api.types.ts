// Shared types matching backend DTOs

export interface CreateTodoCommand {
  title: string;
}

export interface CreateTodoResponse {
  id: string;
}

export interface TodoDto {
  id: string;
  title: string;
  isCompleted: boolean;
  createdAt: string;
  completedAt: string | null;
}

export interface ListTodosResponse {
  todos: TodoDto[];
}
