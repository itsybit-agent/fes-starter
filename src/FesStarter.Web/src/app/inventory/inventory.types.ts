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
