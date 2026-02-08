import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', redirectTo: 'orders', pathMatch: 'full' },
  {
    path: 'orders',
    loadChildren: () => import('./orders/orders.routes').then(m => m.ordersRoutes)
  },
  {
    path: 'inventory',
    loadChildren: () => import('./inventory/inventory.routes').then(m => m.inventoryRoutes)
  }
];
