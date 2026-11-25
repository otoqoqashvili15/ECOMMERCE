export type ProductStatus = 'Active' | 'Inactive';
export type OrderStatus = 'Pending' | 'Paid' | 'Shipped' | 'Delivered' | 'Cancelled';

export interface User {
  id: string;
  displayName: string;
  email: string;
  roles: string[];
}

export interface AuthResponse {
  token: string;
  expiresAtUtc: string;
  user: User;
}

export interface Category {
  id: number;
  name: string;
  description: string;
  imageUrl: string;
  parentId: number | null;
  productCount: number;
}

export interface Product {
  id: number;
  name: string;
  description: string;
  brand: string;
  price: number;
  effectivePrice: number;
  discountPercent: number;
  categoryId: number;
  categoryName: string;
  stock: number;
  minimumStock: number;
  status: ProductStatus;
  images: string[];
  rating: number;
  reviewCount: number;
  createdAtUtc: string;
}

export interface ProductRequest {
  name: string;
  description: string;
  brand: string;
  price: number;
  discountPercent: number;
  categoryId: number;
  stock: number;
  minimumStock: number;
  status: ProductStatus;
  images: string[];
}

export interface CartItem {
  id: number;
  productId: number;
  name: string;
  imageUrl: string;
  unitPrice: number;
  quantity: number;
  stock: number;
  lineTotal: number;
}

export interface CartResponse {
  items: CartItem[];
  subtotal: number;
}

export interface OrderItem {
  productId: number;
  productName: string;
  quantity: number;
  unitPrice: number;
}

export interface StoreOrder {
  id: number;
  status: OrderStatus;
  subtotal: number;
  shippingAmount: number;
  taxAmount: number;
  totalAmount: number;
  shippingName: string;
  shippingAddress: string;
  shippingCity: string;
  shippingCountry: string;
  paymentMethod: string;
  deliveryMethod: 'Standard' | 'Express';
  createdAtUtc: string;
  items: OrderItem[];
}

export interface ProductReview {
  id: number;
  displayName: string;
  rating: number;
  comment: string;
  createdAtUtc: string;
}

export interface CheckoutSettings {
  taxRate: number;
  standardShipping: number;
  expressShipping: number;
  freeShippingThreshold: number;
}

export interface AdminSummary {
  productCount: number;
  orderCount: number;
  lowStockCount: number;
  salesTotal: number;
}

export interface CartLine {
  product: Product;
  quantity: number;
}
