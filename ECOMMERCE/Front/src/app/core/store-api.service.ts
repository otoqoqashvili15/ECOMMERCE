import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, of, switchMap } from 'rxjs';
import {
  AdminSummary,
  AuthResponse,
  CheckoutSettings,
  CartResponse,
  Category,
  OrderStatus,
  Product,
  ProductReview,
  ProductRequest,
  StoreOrder,
  User,
} from './store.models';

@Injectable({ providedIn: 'root' })
export class StoreApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api';
  private readonly tokenKey = 'atelier.access-token';
  private readonly userKey = 'atelier.current-user';

  products(): Observable<Product[]> {
    return this.http.get<Product[]>(`${this.baseUrl}/products`);
  }

  categories(): Observable<Category[]> {
    return this.http.get<Category[]>(`${this.baseUrl}/categories`);
  }

  checkoutSettings(): Observable<CheckoutSettings> {
    return this.http.get<CheckoutSettings>(`${this.baseUrl}/store/checkout-settings`);
  }

  login(email: string, password: string): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.baseUrl}/auth/login`, { email, password });
  }

  register(displayName: string, email: string, password: string): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.baseUrl}/auth/register`, { displayName, email, password });
  }

  currentUser(): User | null {
    try {
      const raw = localStorage.getItem(this.userKey);
      return raw ? (JSON.parse(raw) as User) : null;
    } catch {
      return null;
    }
  }

  saveSession(response: AuthResponse): void {
    localStorage.setItem(this.tokenKey, response.token);
    localStorage.setItem(this.userKey, JSON.stringify(response.user));
  }

  clearSession(): void {
    localStorage.removeItem(this.tokenKey);
    localStorage.removeItem(this.userKey);
  }

  cart(): Observable<CartResponse> {
    return this.http.get<CartResponse>(`${this.baseUrl}/cart`, { headers: this.authHeaders() });
  }

  addCartItem(productId: number, quantity: number): Observable<CartResponse> {
    return this.http.post<CartResponse>(`${this.baseUrl}/cart/items`, { productId, quantity }, { headers: this.authHeaders() });
  }

  setCartQuantity(itemId: number, quantity: number): Observable<CartResponse> {
    return this.http.put<CartResponse>(`${this.baseUrl}/cart/items/${itemId}/quantity`, { quantity }, { headers: this.authHeaders() });
  }

  removeCartItem(itemId: number): Observable<CartResponse> {
    return this.http.delete<CartResponse>(`${this.baseUrl}/cart/items/${itemId}`, { headers: this.authHeaders() });
  }

  updateCartProduct(productId: number, quantity: number): Observable<CartResponse> {
    return this.cart().pipe(switchMap(cart => {
      const item = cart.items.find(line => line.productId === productId);
      return item ? this.setCartQuantity(item.id, quantity) : of(cart);
    }));
  }

  removeCartProduct(productId: number): Observable<CartResponse> {
    return this.cart().pipe(switchMap(cart => {
      const item = cart.items.find(line => line.productId === productId);
      return item ? this.removeCartItem(item.id) : of(cart);
    }));
  }

  placeOrder(details: {
    shippingName: string;
    shippingEmail: string;
    shippingAddress: string;
    shippingCity: string;
    shippingPostalCode: string;
    shippingCountry: string;
    paymentMethod: string;
    deliveryMethod: 'Standard' | 'Express';
  }): Observable<StoreOrder> {
    return this.http.post<StoreOrder>(`${this.baseUrl}/orders`, details, { headers: this.authHeaders() });
  }

  orderHistory(): Observable<StoreOrder[]> {
    return this.http.get<StoreOrder[]>(`${this.baseUrl}/orders`, { headers: this.authHeaders() });
  }

  updateProfile(id: string, displayName: string, email: string): Observable<User> {
    return this.http.put<User>(`${this.baseUrl}/users/${id}`, { displayName, email }, { headers: this.authHeaders() });
  }

  reviews(productId: number): Observable<ProductReview[]> {
    return this.http.get<ProductReview[]>(`${this.baseUrl}/products/${productId}/reviews`);
  }

  addReview(productId: number, rating: number, comment: string): Observable<ProductReview> {
    return this.http.post<ProductReview>(`${this.baseUrl}/products/${productId}/reviews`, { rating, comment }, { headers: this.authHeaders() });
  }

  createProduct(request: ProductRequest): Observable<Product> {
    return this.http.post<Product>(`${this.baseUrl}/products`, request, { headers: this.authHeaders() });
  }

  createCategory(category: Pick<Category, 'name' | 'description' | 'imageUrl'>): Observable<Category> {
    return this.http.post<Category>(`${this.baseUrl}/categories`, { ...category, parentId: null }, { headers: this.authHeaders() });
  }

  adminOrders(): Observable<StoreOrder[]> {
    return this.http.get<StoreOrder[]>(`${this.baseUrl}/orders`, { headers: this.authHeaders() });
  }

  updateProduct(id: number, request: ProductRequest): Observable<Product> {
    return this.http.put<Product>(`${this.baseUrl}/products/${id}`, request, { headers: this.authHeaders() });
  }

  archiveProduct(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/products/${id}`, { headers: this.authHeaders() });
  }

  adminSummary(): Observable<AdminSummary> {
    return this.http.get<AdminSummary>(`${this.baseUrl}/admin/summary`, { headers: this.authHeaders() });
  }

  lowStock(): Observable<Product[]> {
    return this.http.get<Product[]>(`${this.baseUrl}/admin/inventory/low-stock`, { headers: this.authHeaders() });
  }

  setOrderStatus(id: number, status: OrderStatus): Observable<StoreOrder> {
    return this.http.put<StoreOrder>(`${this.baseUrl}/orders/${id}/status`, { status }, { headers: this.authHeaders() });
  }

  private authHeaders(): HttpHeaders {
    const token = localStorage.getItem(this.tokenKey);
    return token ? new HttpHeaders({ Authorization: `Bearer ${token}` }) : new HttpHeaders();
  }
}
