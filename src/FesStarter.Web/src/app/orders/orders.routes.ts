import { Component, ViewChild } from '@angular/core';
import { Routes } from '@angular/router';
import { PlaceOrderComponent } from './place-order.component';
import { OrderListComponent } from './order-list.component';

@Component({
  standalone: true,
  imports: [PlaceOrderComponent, OrderListComponent],
  template: `
    <div class="orders-page">
      <h2>Orders</h2>
      <app-place-order (orderPlaced)="orderList.loadOrders()" />
      <app-order-list #orderList />
    </div>
  `,
  styles: [`.orders-page { padding: 1rem; }`]
})
export class OrdersPage {
  @ViewChild(OrderListComponent) orderList!: OrderListComponent;
}

export const ordersRoutes: Routes = [
  { path: '', component: OrdersPage }
];
