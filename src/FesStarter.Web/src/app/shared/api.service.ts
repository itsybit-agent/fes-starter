import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { PlaceOrderCommand, PlaceOrderResponse, OrderDto, InitializeStockRequest, StockDto } from './api.types';
import { environment } from '../../environments/environment';

@Injectable({ providedIn: 'root' })
export class ApiService {
  private readonly baseUrl = environment.apiUrl;

  constructor(private http: HttpClient) {}

  // Orders
  placeOrder(command: PlaceOrderCommand): Observable<PlaceOrderResponse> {
    return this.http.post<PlaceOrderResponse>(`${this.baseUrl}/orders`, command);
  }

  shipOrder(orderId: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/orders/${orderId}/ship`, {});
  }

  listOrders(): Observable<OrderDto[]> {
    return this.http.get<OrderDto[]>(`${this.baseUrl}/orders`);
  }

  // Inventory
  initializeStock(productId: string, request: InitializeStockRequest): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/products/${productId}/stock`, request);
  }

  getStock(productId: string): Observable<StockDto> {
    return this.http.get<StockDto>(`${this.baseUrl}/products/${productId}/stock`);
  }

  listStock(): Observable<StockDto[]> {
    return this.http.get<StockDto[]>(`${this.baseUrl}/products/stock`);
  }
}
