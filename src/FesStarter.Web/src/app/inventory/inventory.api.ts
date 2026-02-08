import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { InitializeStockRequest, StockDto } from './inventory.types';
import { environment } from '../../environments/environment';

@Injectable({ providedIn: 'root' })
export class InventoryApi {
  private readonly baseUrl = environment.apiUrl;

  constructor(private http: HttpClient) {}

  initializeStock(productId: string, request: InitializeStockRequest, idempotencyKey?: string): Observable<void> {
    const key = idempotencyKey || crypto.randomUUID();
    return this.http.post<void>(
      `${this.baseUrl}/products/${productId}/stock`,
      request,
      { headers: { 'Idempotency-Key': key } }
    );
  }

  getStock(productId: string): Observable<StockDto> {
    return this.http.get<StockDto>(`${this.baseUrl}/products/${productId}/stock`);
  }

  listStock(): Observable<StockDto[]> {
    return this.http.get<StockDto[]>(`${this.baseUrl}/products/stock`);
  }
}
