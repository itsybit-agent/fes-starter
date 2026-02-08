import { Component, signal, output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { InventoryApi } from './inventory.api';
import { ToastService } from '../shared/toast.service';

@Component({
  selector: 'app-add-product',
  standalone: true,
  imports: [FormsModule],
  template: `
    <h3>Add Product</h3>
    <div class="add-product">
      <input type="text" [ngModel]="productId()" (ngModelChange)="productId.set($event)" placeholder="Product ID (e.g. widget-001)">
      <input type="text" [ngModel]="productName()" (ngModelChange)="productName.set($event)" placeholder="Product Name">
      <input type="number" [ngModel]="initialQuantity()" (ngModelChange)="initialQuantity.set($event)" min="0" placeholder="Initial Qty">
      <button (click)="addProduct()" [disabled]="!productId() || !productName()">
        Add Product
      </button>
    </div>
  `,
  styles: [`
    .add-product { margin-bottom: 2rem; display: flex; gap: 0.5rem; align-items: center; flex-wrap: wrap; }
    input { padding: 0.5rem; }
    input[type="number"] { width: 100px; }
    button { padding: 0.5rem 1rem; cursor: pointer; }
  `]
})
export class AddProductComponent {
  productId = signal('');
  productName = signal('');
  initialQuantity = signal(100);

  productAdded = output<void>();

  constructor(
    private api: InventoryApi,
    private toast: ToastService
  ) {}

  addProduct() {
    if (!this.productId() || !this.productName()) return;
    this.api.initializeStock(this.productId(), {
      productName: this.productName(),
      initialQuantity: this.initialQuantity()
    }).subscribe({
      next: () => {
        this.productId.set('');
        this.productName.set('');
        this.initialQuantity.set(100);
        this.productAdded.emit();
        this.toast.success('Product added successfully');
      },
      error: err => this.toast.error('Failed to add product')
    });
  }
}
