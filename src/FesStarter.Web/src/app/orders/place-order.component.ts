import { Component, OnInit, signal, output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { OrdersApi } from './orders.api';
import { InventoryApi } from '../inventory/inventory.api';
import { StockDto } from '../inventory/inventory.types';

@Component({
  selector: 'app-place-order',
  standalone: true,
  imports: [FormsModule],
  template: `
    @if (products().length > 0) {
      <h3>Place Order</h3>
      <div class="place-order">
        <select [ngModel]="selectedProductId()" (ngModelChange)="selectedProductId.set($event)">
          <option value="">Select product...</option>
          @for (p of products(); track p.productId) {
            <option [value]="p.productId">
              {{ p.productName }} ({{ p.availableQuantity }} available)
            </option>
          }
        </select>
        <input type="number" [ngModel]="quantity()" (ngModelChange)="quantity.set($event)" min="1" placeholder="Qty">
        <button (click)="placeOrder()" [disabled]="!selectedProductId() || quantity() < 1">
          Place Order
        </button>
      </div>
    } @else {
      <p class="hint">Add products in Inventory first</p>
    }
  `,
  styles: [`
    .place-order { margin-bottom: 2rem; display: flex; gap: 0.5rem; align-items: center; flex-wrap: wrap; }
    select, input { padding: 0.5rem; }
    input[type="number"] { width: 80px; }
    button { padding: 0.5rem 1rem; cursor: pointer; }
    .hint { color: #666; font-style: italic; }
  `]
})
export class PlaceOrderComponent implements OnInit {
  products = signal<StockDto[]>([]);
  selectedProductId = signal('');
  quantity = signal(1);

  orderPlaced = output<void>();

  constructor(
    private ordersApi: OrdersApi,
    private inventoryApi: InventoryApi
  ) {}

  ngOnInit() {
    this.loadProducts();
  }

  loadProducts() {
    this.inventoryApi.listStock().subscribe(data => this.products.set(data));
  }

  placeOrder() {
    if (!this.selectedProductId() || this.quantity() < 1) return;
    this.ordersApi.placeOrder({
      productId: this.selectedProductId(),
      quantity: this.quantity()
    }).subscribe(() => {
      this.selectedProductId.set('');
      this.quantity.set(1);
      this.loadProducts();
      this.orderPlaced.emit();
    });
  }
}
