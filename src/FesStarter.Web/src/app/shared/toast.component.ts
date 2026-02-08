import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ToastService } from './toast.service';

@Component({
  selector: 'app-toast',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="toast-container">
      @for (toast of toasts(); track toast.id) {
        <div [class]="'toast toast-' + toast.type">
          <span>{{ toast.message }}</span>
          <button (click)="remove(toast.id)" class="close">×</button>
        </div>
      }
    </div>
  `,
  styles: [`
    .toast-container {
      position: fixed;
      top: 1rem;
      right: 1rem;
      z-index: 9999;
      display: flex;
      flex-direction: column;
      gap: 0.5rem;
      max-width: 400px;
    }

    .toast {
      display: flex;
      align-items: center;
      justify-content: space-between;
      padding: 1rem;
      border-radius: 4px;
      background: #fff;
      box-shadow: 0 2px 8px rgba(0, 0, 0, 0.15);
      animation: slideIn 0.3s ease-out;
      gap: 1rem;
    }

    .toast-error {
      background: #f8d7da;
      color: #721c24;
      border-left: 4px solid #f5c6cb;
    }

    .toast-success {
      background: #d4edda;
      color: #155724;
      border-left: 4px solid #c3e6cb;
    }

    .toast-info {
      background: #d1ecf1;
      color: #0c5460;
      border-left: 4px solid #bee5eb;
    }

    .toast-warning {
      background: #fff3cd;
      color: #856404;
      border-left: 4px solid #ffeaa7;
    }

    .close {
      background: none;
      border: none;
      font-size: 1.5rem;
      cursor: pointer;
      opacity: 0.7;
      transition: opacity 0.2s;
      padding: 0;
      line-height: 1;
    }

    .close:hover {
      opacity: 1;
    }

    @keyframes slideIn {
      from {
        transform: translateX(400px);
        opacity: 0;
      }
      to {
        transform: translateX(0);
        opacity: 1;
      }
    }
  `]
})
export class ToastComponent {
  private toastService = inject(ToastService);

  get toasts() {
    return this.toastService.toasts;
  }

  remove(id: string): void {
    this.toastService.remove(id);
  }
}
