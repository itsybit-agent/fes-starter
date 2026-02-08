import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { PlaceOrderCommand, PlaceOrderResponse, OrderDto } from './orders.types';
import { environment } from '../../environments/environment';

@Injectable({ providedIn: 'root' })
export class OrdersApi {
  private readonly baseUrl = environment.apiUrl;

  constructor(private http: HttpClient) {}

  placeOrder(command: PlaceOrderCommand): Observable<PlaceOrderResponse> {
    return this.http.post<PlaceOrderResponse>(`${this.baseUrl}/orders`, command);
  }

  shipOrder(orderId: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/orders/${orderId}/ship`, {});
  }

  listOrders(): Observable<OrderDto[]> {
    return this.http.get<OrderDto[]>(`${this.baseUrl}/orders`);
  }
}
