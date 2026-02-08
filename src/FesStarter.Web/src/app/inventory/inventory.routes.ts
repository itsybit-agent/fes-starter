import { Component, ViewChild } from '@angular/core';
import { Routes } from '@angular/router';
import { AddProductComponent } from './add-product.component';
import { StockListComponent } from './stock-list.component';

@Component({
  standalone: true,
  imports: [AddProductComponent, StockListComponent],
  template: `
    <div class="inventory-page">
      <h2>Inventory</h2>
      <app-add-product (productAdded)="stockList.loadStock()" />
      <app-stock-list #stockList />
    </div>
  `,
  styles: [`.inventory-page { padding: 1rem; }`]
})
export class InventoryPage { }

export const inventoryRoutes: Routes = [
  { path: '', component: InventoryPage }
];
