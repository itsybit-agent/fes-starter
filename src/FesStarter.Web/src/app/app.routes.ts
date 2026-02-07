import { Routes } from '@angular/router';
import { OrdersComponent } from './features/orders/orders.component';
import { InventoryComponent } from './features/inventory/inventory.component';

export const routes: Routes = [
  { path: '', redirectTo: 'orders', pathMatch: 'full' },
  { path: 'orders', component: OrdersComponent },
  { path: 'inventory', component: InventoryComponent }
];
