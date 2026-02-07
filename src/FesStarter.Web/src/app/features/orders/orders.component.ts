import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../shared/api.service';
import { OrderDto, StockDto } from '../../shared/api.types';

@Component({
  selector: 'app-orders',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="orders-container">
      <h2>Orders</h2>
      
      <div class="place-order" *ngIf="products.length > 0">
        <h3>Place Order</h3>
        <select [(ngModel)]="selectedProductId">
          <option value="">Select product...</option>
          <option *ngFor="let p of products" [value]="p.productId">
            {{p.productName}} ({{p.availableQuantity}} available)
          </option>
        </select>
        <input type="number" [(ngModel)]="quantity" min="1" placeholder="Qty">
        <button (click)="placeOrder()" [disabled]="!selectedProductId || quantity < 1">
          Place Order
        </button>
      </div>
      <p *ngIf="products.length === 0" class="hint">Add products in Inventory first</p>

      <div class="order-list">
        <h3>Order History</h3>
        <table *ngIf="orders.length > 0">
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
            <tr *ngFor="let order of orders" [class]="order.status.toLowerCase()">
              <td>{{order.orderId | slice:0:8}}</td>
              <td>{{order.productId | slice:0:8}}</td>
              <td>{{order.quantity}}</td>
              <td>{{order.status}}</td>
              <td>
                <button *ngIf="order.status === 'Placed'" (click)="shipOrder(order.orderId)">
                  Ship
                </button>
                <span *ngIf="order.status === 'Shipped'">✓</span>
                <span *ngIf="order.status === 'Pending'">⏳</span>
              </td>
            </tr>
          </tbody>
        </table>
        <p *ngIf="orders.length === 0">No orders yet</p>
      </div>
    </div>
  `,
  styles: [`
    .orders-container { padding: 1rem; }
    .place-order { margin-bottom: 2rem; display: flex; gap: 0.5rem; align-items: center; flex-wrap: wrap; }
    select, input { padding: 0.5rem; }
    input[type="number"] { width: 80px; }
    button { padding: 0.5rem 1rem; cursor: pointer; }
    table { width: 100%; border-collapse: collapse; }
    th, td { padding: 0.5rem; text-align: left; border-bottom: 1px solid #ddd; }
    .pending { background: #fff3cd; }
    .placed { background: #d1ecf1; }
    .shipped { background: #d4edda; }
    .hint { color: #666; font-style: italic; }
  `]
})
export class OrdersComponent implements OnInit {
  orders: OrderDto[] = [];
  products: StockDto[] = [];
  selectedProductId = '';
  quantity = 1;

  constructor(private api: ApiService) {}

  ngOnInit() {
    this.loadOrders();
    this.loadProducts();
  }

  loadOrders() {
    this.api.listOrders().subscribe(orders => this.orders = orders);
  }

  loadProducts() {
    this.api.listStock().subscribe(products => this.products = products);
  }

  placeOrder() {
    if (!this.selectedProductId || this.quantity < 1) return;
    this.api.placeOrder({ productId: this.selectedProductId, quantity: this.quantity }).subscribe(() => {
      this.loadOrders();
      this.loadProducts();
      this.selectedProductId = '';
      this.quantity = 1;
    });
  }

  shipOrder(orderId: string) {
    this.api.shipOrder(orderId).subscribe(() => {
      this.loadOrders();
      this.loadProducts();
    });
  }
}
