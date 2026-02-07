// Orders
export interface PlaceOrderCommand {
  productId: string;
  quantity: number;
}

export interface PlaceOrderResponse {
  orderId: string;
}

export interface OrderDto {
  orderId: string;
  productId: string;
  quantity: number;
  status: string;
  placedAt: string;
  shippedAt?: string;
}

// Inventory
export interface InitializeStockRequest {
  productName: string;
  initialQuantity: number;
}

export interface StockDto {
  productId: string;
  productName: string;
  quantityOnHand: number;
  quantityReserved: number;
  availableQuantity: number;
}
