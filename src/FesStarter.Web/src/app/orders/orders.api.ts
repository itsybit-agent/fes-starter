import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { PlaceOrderCommand, PlaceOrderResponse, OrderDto } from './orders.types';
import { environment } from '../../environments/environment';

@Injectable({ providedIn: 'root' })
export class OrdersApi {
  private readonly baseUrl = environment.apiUrl;

  constructor(private http: HttpClient) {}

  placeOrder(command: PlaceOrderCommand, idempotencyKey?: string): Observable<PlaceOrderResponse> {
    const key = idempotencyKey || crypto.randomUUID();
    return this.http.post<PlaceOrderResponse>(
      `${this.baseUrl}/orders`,
      command,
      { headers: { 'Idempotency-Key': key } }
    );
  }

  shipOrder(orderId: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/orders/${orderId}/ship`, {});
  }

  listOrders(): Observable<OrderDto[]> {
    return this.http.get<OrderDto[]>(`${this.baseUrl}/orders`);
  }
}
