import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../shared/api.service';
import { StockDto } from '../../shared/api.types';

@Component({
  selector: 'app-inventory',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="inventory-container">
      <h2>Inventory</h2>
      
      <div class="add-product">
        <h3>Add Product</h3>
        <input type="text" [(ngModel)]="productId" placeholder="Product ID (e.g. widget-001)">
        <input type="text" [(ngModel)]="productName" placeholder="Product Name">
        <input type="number" [(ngModel)]="initialQuantity" min="0" placeholder="Initial Qty">
        <button (click)="addProduct()" [disabled]="!productId || !productName">
          Add Product
        </button>
      </div>

      <div class="stock-list">
        <h3>Stock Levels</h3>
        <table *ngIf="stocks.length > 0">
          <thead>
            <tr>
              <th>Product ID</th>
              <th>Name</th>
              <th>On Hand</th>
              <th>Reserved</th>
              <th>Available</th>
            </tr>
          </thead>
          <tbody>
            <tr *ngFor="let stock of stocks" [class.low-stock]="stock.availableQuantity < 5">
              <td>{{stock.productId | slice:0:12}}</td>
              <td>{{stock.productName}}</td>
              <td>{{stock.quantityOnHand}}</td>
              <td>{{stock.quantityReserved}}</td>
              <td>{{stock.availableQuantity}}</td>
            </tr>
          </tbody>
        </table>
        <p *ngIf="stocks.length === 0">No products in inventory</p>
      </div>
    </div>
  `,
  styles: [`
    .inventory-container { padding: 1rem; }
    .add-product { margin-bottom: 2rem; display: flex; gap: 0.5rem; align-items: center; flex-wrap: wrap; }
    input { padding: 0.5rem; }
    input[type="number"] { width: 100px; }
    button { padding: 0.5rem 1rem; cursor: pointer; }
    table { width: 100%; border-collapse: collapse; }
    th, td { padding: 0.5rem; text-align: left; border-bottom: 1px solid #ddd; }
    .low-stock { background: #f8d7da; }
  `]
})
export class InventoryComponent implements OnInit {
  stocks: StockDto[] = [];
  productId = '';
  productName = '';
  initialQuantity = 100;

  constructor(private api: ApiService) {}

  ngOnInit() {
    this.loadStock();
  }

  loadStock() {
    this.api.listStock().subscribe(stocks => this.stocks = stocks);
  }

  addProduct() {
    if (!this.productId || !this.productName) return;
    this.api.initializeStock(this.productId, {
      productName: this.productName,
      initialQuantity: this.initialQuantity
    }).subscribe(() => {
      this.loadStock();
      this.productId = '';
      this.productName = '';
      this.initialQuantity = 100;
    });
  }
}
