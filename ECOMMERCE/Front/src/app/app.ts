import { CommonModule, CurrencyPipe, DatePipe } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { catchError, concatMap, from, of, switchMap, toArray } from 'rxjs';
import { AdminPanelComponent } from './admin/admin-panel.component';
import { CartStore } from './core/cart.store';
import { StoreApiService } from './core/store-api.service';
import { CartResponse, Category, CheckoutSettings, Product, ProductReview, StoreOrder, User } from './core/store.models';
import { ProductCardComponent } from './products/product-card.component';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, FormsModule, CurrencyPipe, DatePipe, ProductCardComponent, AdminPanelComponent],
  templateUrl: './app.html',
  styleUrl: './app.scss',
})
export class App implements OnInit {
  private readonly api = inject(StoreApiService);
  readonly cart = inject(CartStore);

  readonly products = signal<Product[]>([]);
  readonly categories = signal<Category[]>([]);
  readonly loading = signal(true);
  readonly catalogError = signal('');
  readonly search = signal('');
  readonly selectedCategory = signal<number | null>(null);
  readonly sort = signal('featured');
  readonly maxPrice = signal<number | null>(null);
  readonly cartOpen = signal(false);
  readonly authOpen = signal(false);
  readonly accountOpen = signal(false);
  readonly authMode = signal<'login' | 'register'>('login');
  readonly session = signal<User | null>(this.api.currentUser());
  readonly manageMode = signal(false);
  readonly notice = signal('');
  readonly checkoutBusy = signal(false);
  readonly orderHistory = signal<StoreOrder[]>([]);
  readonly checkoutSettings = signal<CheckoutSettings>({ taxRate: 0.08, standardShipping: 5, expressShipping: 15, freeShippingThreshold: 100 });
  readonly delivery = signal<'Standard' | 'Express'>('Standard');
  readonly reviewProduct = signal<Product | null>(null);
  readonly reviews = signal<ProductReview[]>([]);
  readonly reviewBusy = signal(false);
  readonly reviewRating = signal(5);
  readonly reviewComment = signal('');
  readonly visibleProducts = computed(() => {
    const term = this.search().trim().toLowerCase();
    let list = this.products().filter(product => product.status === 'Active');
    if (term) {
      list = list.filter(product => `${product.name} ${product.description} ${product.brand} ${product.categoryName}`.toLowerCase().includes(term));
    }
    if (this.selectedCategory() !== null) list = list.filter(product => product.categoryId === this.selectedCategory());
    if (this.maxPrice() !== null) list = list.filter(product => product.effectivePrice <= this.maxPrice()!);
    switch (this.sort()) {
      case 'price-low': return [...list].sort((a, b) => a.effectivePrice - b.effectivePrice);
      case 'price-high': return [...list].sort((a, b) => b.effectivePrice - a.effectivePrice);
      case 'newest': return [...list].sort((a, b) => b.createdAtUtc.localeCompare(a.createdAtUtc));
      case 'rating': return [...list].sort((a, b) => b.rating - a.rating);
      default: return list;
    }
  });
  readonly isManager = computed(() => this.session()?.roles.some(role => role === 'Administrator' || role === 'StoreManager') ?? false);
  readonly estimatedShipping = computed(() => this.delivery() === 'Express'
    ? this.checkoutSettings().expressShipping
    : this.cart.subtotal() >= this.checkoutSettings().freeShippingThreshold ? 0 : this.checkoutSettings().standardShipping);
  readonly estimatedTax = computed(() => Math.round(this.cart.subtotal() * this.checkoutSettings().taxRate * 100) / 100);
  readonly estimatedTotal = computed(() => this.cart.subtotal() + this.estimatedShipping() + this.estimatedTax());

  authName = '';
  authEmail = '';
  authPassword = '';
  authError = '';
  authBusy = false;
  checkoutAfterLogin = false;
  checkout = {
    shippingName: '',
    shippingEmail: '',
    shippingAddress: '',
    shippingCity: '',
    shippingPostalCode: '',
    shippingCountry: 'United States',
    paymentMethod: 'Cash on delivery',
    deliveryMethod: 'Standard' as 'Standard' | 'Express',
  };
  profileName = '';
  profileEmail = '';

  ngOnInit(): void {
    this.api.checkoutSettings().subscribe({ next: settings => this.checkoutSettings.set(settings) });
    this.loadCatalog();
  }

  setDelivery(value: 'Standard' | 'Express'): void {
    this.delivery.set(value);
    this.checkout.deliveryMethod = value;
  }

  loadCatalog(): void {
    this.loading.set(true);
    this.catalogError.set('');
    this.api.products().pipe(
      switchMap(products => this.api.categories().pipe(switchMap(categories => of({ products, categories })))),
      catchError(error => {
        this.catalogError.set(error?.error?.message ?? 'We could not reach the store right now. Check that the API is running.');
        return of({ products: [] as Product[], categories: [] as Category[] });
      }),
    ).subscribe(result => {
      this.products.set(result.products);
      this.categories.set(result.categories);
      this.loading.set(false);
    });
  }

  addToCart(product: Product): void {
    if (product.stock < 1) return;
    this.cart.add(product);
    if (this.session()) {
      this.api.addCartItem(product.id, 1).subscribe({
        error: error => this.announce(error?.error?.message ?? 'Your basket is saved on this device, but could not sync to your account.'),
      });
    }
    this.cartOpen.set(true);
    this.announce(`${product.name} added to your bag.`);
  }

  updateQuantity(productId: number, quantity: number): void {
    const line = this.cart.lines().find(item => item.product.id === productId);
    if (!line) return;
    this.cart.setQuantity(productId, quantity);
    if (this.session()) {
      const updated = this.cart.lines().find(item => item.product.id === productId);
      if (updated) this.api.updateCartProduct(productId, updated.quantity).subscribe();
    }
  }

  removeFromCart(productId: number): void {
    this.cart.remove(productId);
    if (this.session()) this.api.removeCartProduct(productId).subscribe();
  }

  openReviews(product: Product): void {
    this.reviewProduct.set(product);
    this.reviewRating.set(5);
    this.reviewComment.set('');
    this.api.reviews(product.id).subscribe({ next: reviews => this.reviews.set(reviews) });
  }

  submitReview(): void {
    const product = this.reviewProduct();
    if (!product) return;
    if (!this.session()) {
      this.reviewProduct.set(null);
      this.openAuth('login');
      return;
    }
    this.reviewBusy.set(true);
    this.api.addReview(product.id, this.reviewRating(), this.reviewComment()).subscribe({
      next: () => {
        this.reviewBusy.set(false);
        this.api.reviews(product.id).subscribe({ next: reviews => this.reviews.set(reviews) });
        this.loadCatalog();
        this.announce('Thanks for sharing your thoughts.');
      },
      error: error => {
        this.reviewBusy.set(false);
        this.announce(error?.error?.message ?? 'Could not save your review.');
      },
    });
  }

  openAccount(): void {
    if (!this.session()) {
      this.openAuth('login');
      return;
    }
    this.accountOpen.set(true);
    const user = this.session()!;
    this.profileName = user.displayName;
    this.profileEmail = user.email;
    this.api.orderHistory().subscribe({ next: orders => this.orderHistory.set(orders) });
  }

  saveProfile(): void {
    const user = this.session();
    if (!user) return;
    this.api.updateProfile(user.id, this.profileName, this.profileEmail).subscribe({
      next: updated => {
        this.session.set(updated);
        localStorage.setItem('atelier.current-user', JSON.stringify(updated));
        this.announce('Profile details updated.');
      },
      error: error => this.announce(error?.error?.message ?? 'Could not update your profile.'),
    });
  }

  signOut(): void {
    this.api.clearSession();
    this.session.set(null);
    this.accountOpen.set(false);
    this.manageMode.set(false);
    this.announce('You have signed out.');
  }

  openAuth(mode: 'login' | 'register'): void {
    this.authMode.set(mode);
    this.authError = '';
    this.authPassword = '';
    this.authOpen.set(true);
    this.accountOpen.set(false);
  }

  submitAuth(): void {
    this.authError = '';
    this.authBusy = true;
    const request = this.authMode() === 'register'
      ? this.api.register(this.authName, this.authEmail, this.authPassword)
      : this.api.login(this.authEmail, this.authPassword);
    request.subscribe({
      next: response => {
        this.api.saveSession(response);
        this.session.set(response.user);
        this.checkout.shippingName = response.user.displayName;
        this.checkout.shippingEmail = response.user.email;
        this.authOpen.set(false);
        this.authBusy = false;
        this.announce(`Welcome, ${response.user.displayName.split(' ')[0]}.`);
        this.syncCartAfterAuth(() => {
          if (this.checkoutAfterLogin) {
            this.checkoutAfterLogin = false;
            this.startCheckout();
          }
        });
      },
      error: error => {
        this.authBusy = false;
        this.authError = error?.error?.message ?? 'We could not sign you in. Check your details and try again.';
      },
    });
  }

  startCheckout(): void {
    if (!this.cart.lines().length) return;
    if (!this.session()) {
      this.checkoutAfterLogin = true;
      this.openAuth('login');
      return;
    }
    const user = this.session()!;
    if (!this.checkout.shippingName) this.checkout.shippingName = user.displayName;
    if (!this.checkout.shippingEmail) this.checkout.shippingEmail = user.email;
    this.checkoutBusy.set(true);
    this.api.placeOrder(this.checkout).subscribe({
      next: order => {
        this.checkoutBusy.set(false);
        this.cart.clear();
        this.cartOpen.set(false);
        this.loadCatalog();
        this.announce(`Order #${order.id} is placed. We’ll send updates to ${this.checkout.shippingEmail}.`);
      },
      error: error => {
        this.checkoutBusy.set(false);
        this.announce(error?.error?.message ?? 'We could not place that order. Please review your details.');
      },
    });
  }

  toggleManageMode(): void {
    this.manageMode.update(value => !value);
    this.accountOpen.set(false);
    if (this.manageMode()) this.loadCatalog();
  }

  announce(message: string): void {
    this.notice.set(message);
    window.setTimeout(() => this.notice.set(''), 3600);
  }

  private syncCartAfterAuth(done: () => void): void {
    this.api.cart().subscribe({
      next: serverCart => {
        const localLines = this.cart.lines();
        const missing = localLines.filter(line => !serverCart.items.some(item => item.productId === line.product.id));
        const sync$ = missing.length
          ? from(missing).pipe(concatMap(line => this.api.addCartItem(line.product.id, line.quantity)), toArray(), switchMap(() => this.api.cart()))
          : of(serverCart);
        sync$.subscribe({
          next: cart => {
            this.hydrateCart(cart);
            done();
          },
          error: () => done(),
        });
      },
      error: () => done(),
    });
  }

  private hydrateCart(response: CartResponse): void {
    const products = this.products();
    this.cart.replace(response.items.flatMap(item => {
      const product = products.find(candidate => candidate.id === item.productId);
      if (!product) return [];
      return [{ product: { ...product, effectivePrice: item.unitPrice, stock: item.stock }, quantity: item.quantity }];
    }));
  }

}
