import { Component, OnInit, signal } from '@angular/core';
import { OrdersApi } from './orders.api';
import { OrderDto } from './orders.types';
import { ToastService } from '../shared/toast.service';

@Component({
  selector: 'app-order-list',
  standalone: true,
  template: `
    <h3>Order History</h3>
    @if (orders().length > 0) {
      <table>
        <thead>
          <tr>
            <th>Order ID</th>
            <th>Product</th>
            <th>Qty</th>
            <th>Status</th>
            <th>Actions</th>
          </tr>
        </thead>
        <tbody>
          @for (order of orders(); track order.orderId) {
            <tr [class]="order.status.toLowerCase()">
              <td>{{ order.orderId.slice(0, 8) }}</td>
              <td>{{ order.productId.slice(0, 8) }}</td>
              <td>{{ order.quantity }}</td>
              <td>{{ order.status }}</td>
              <td>
                @if (order.status === 'Placed') {
                  <button (click)="shipOrder(order.orderId)">Ship</button>
                } @else if (order.status === 'Shipped') {
                  <span>✓</span>
                } @else {
                  <span>⏳</span>
                }
              </td>
            </tr>
          }
        </tbody>
      </table>
    } @else {
      <p>No orders yet</p>
    }
  `,
  styles: [`
    table { width: 100%; border-collapse: collapse; }
    th, td { padding: 0.5rem; text-align: left; border-bottom: 1px solid #ddd; }
    .pending { background: #fff3cd; }
    .placed { background: #d1ecf1; }
    .shipped { background: #d4edda; }
    button { padding: 0.25rem 0.75rem; cursor: pointer; }
  `]
})
export class OrderListComponent implements OnInit {
  orders = signal<OrderDto[]>([]);

  constructor(
    private api: OrdersApi,
    private toast: ToastService
  ) {}

  ngOnInit() {
    this.loadOrders();
  }

  loadOrders() {
    this.api.listOrders().subscribe({
      next: data => this.orders.set(data),
      error: err => this.toast.error('Failed to load orders')
    });
  }

  shipOrder(orderId: string) {
    this.api.shipOrder(orderId).subscribe({
      next: () => {
        this.loadOrders();
        this.toast.success('Order shipped successfully');
      },
      error: err => this.toast.error('Failed to ship order')
    });
  }
}
