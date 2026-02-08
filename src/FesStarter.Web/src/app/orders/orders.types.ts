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
