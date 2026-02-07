import { Component, OnInit, signal } from '@angular/core';
import { InventoryApi } from './inventory.api';
import { StockDto } from './inventory.types';

@Component({
  selector: 'app-stock-list',
  standalone: true,
  template: `
    <h3>Stock Levels</h3>
    @if (stocks().length > 0) {
      <table>
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
          @for (stock of stocks(); track stock.productId) {
            <tr [class.low-stock]="stock.availableQuantity < 5">
              <td>{{ stock.productId.slice(0, 12) }}</td>
              <td>{{ stock.productName }}</td>
              <td>{{ stock.quantityOnHand }}</td>
              <td>{{ stock.quantityReserved }}</td>
              <td>{{ stock.availableQuantity }}</td>
            </tr>
          }
        </tbody>
      </table>
    } @else {
      <p>No products in inventory</p>
    }
  `,
  styles: [`
    table { width: 100%; border-collapse: collapse; }
    th, td { padding: 0.5rem; text-align: left; border-bottom: 1px solid #ddd; }
    .low-stock { background: #f8d7da; }
  `]
})
export class StockListComponent implements OnInit {
  stocks = signal<StockDto[]>([]);

  constructor(private api: InventoryApi) {}

  ngOnInit() {
    this.loadStock();
  }

  loadStock() {
    this.api.listStock().subscribe(data => this.stocks.set(data));
  }
}
