import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, OnInit, inject, input, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AdminSummary, Category, OrderStatus, Product, ProductRequest, StoreOrder } from '../core/store.models';
import { StoreApiService } from '../core/store-api.service';

@Component({
  selector: 'app-admin-panel',
  standalone: true,
  imports: [FormsModule, CurrencyPipe, DatePipe],
  templateUrl: './admin-panel.component.html',
  styleUrl: './admin-panel.component.scss',
})
export class AdminPanelComponent implements OnInit {
  readonly products = input<Product[]>([]);
  readonly categories = input<Category[]>([]);
  readonly refreshRequested = output<void>();
  readonly notice = output<string>();

  private readonly api = inject(StoreApiService);
  readonly summary = signal<AdminSummary | null>(null);
  readonly orders = signal<StoreOrder[]>([]);
  readonly saving = signal(false);
  readonly editingId = signal<number | null>(null);
  readonly formError = signal('');
  readonly statuses: OrderStatus[] = ['Pending', 'Paid', 'Shipped', 'Delivered', 'Cancelled'];

  name = '';
  description = '';
  brand = '';
  price: number | null = null;
  discountPercent: number | null = 0;
  categoryId: number | null = null;
  stock: number | null = 0;
  minimumStock: number | null = 1;
  imageUrl = '';
  categoryName = '';
  categoryDescription = '';

  ngOnInit(): void {
    this.refreshDashboard();
  }

  refreshDashboard(): void {
    this.api.adminSummary().subscribe({ next: value => this.summary.set(value) });
    this.api.adminOrders().subscribe({ next: value => this.orders.set(value) });
  }

  edit(product: Product): void {
    this.editingId.set(product.id);
    this.name = product.name;
    this.description = product.description;
    this.brand = product.brand;
    this.price = product.price;
    this.discountPercent = product.discountPercent;
    this.categoryId = product.categoryId;
    this.stock = product.stock;
    this.minimumStock = product.minimumStock;
    this.imageUrl = product.images[0] ?? '';
    this.formError.set('');
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }

  resetProductForm(): void {
    this.editingId.set(null);
    this.name = '';
    this.description = '';
    this.brand = '';
    this.price = null;
    this.discountPercent = 0;
    this.categoryId = this.categories()[0]?.id ?? null;
    this.stock = 0;
    this.minimumStock = 1;
    this.imageUrl = '';
    this.formError.set('');
  }

  saveProduct(): void {
    if (!this.name.trim() || !this.categoryId || this.price === null || this.price < 0) {
      this.formError.set('Add a product name, category, and valid price.');
      return;
    }
    const request: ProductRequest = {
      name: this.name.trim(),
      description: this.description.trim(),
      brand: this.brand.trim(),
      price: this.price,
      discountPercent: this.discountPercent ?? 0,
      categoryId: this.categoryId,
      stock: this.stock ?? 0,
      minimumStock: this.minimumStock ?? 0,
      status: 'Active',
      images: this.imageUrl.trim() ? [this.imageUrl.trim()] : [],
    };
    this.saving.set(true);
    const id = this.editingId();
    const operation = id ? this.api.updateProduct(id, request) : this.api.createProduct(request);
    operation.subscribe({
      next: product => {
        this.saving.set(false);
        this.notice.emit(`${product.name} ${id ? 'updated' : 'added'} to the catalog.`);
        this.resetProductForm();
        this.refreshRequested.emit();
        this.refreshDashboard();
      },
      error: err => {
        this.saving.set(false);
        this.formError.set(err?.error?.message ?? 'Could not save this product. Check the fields and try again.');
      },
    });
  }

  archive(product: Product): void {
    if (!window.confirm(`Archive “${product.name}” from the storefront?`)) return;
    this.api.archiveProduct(product.id).subscribe({
      next: () => {
        this.notice.emit(`${product.name} was archived.`);
        this.refreshRequested.emit();
        this.refreshDashboard();
      },
      error: err => this.notice.emit(err?.error?.message ?? 'Could not archive this product.'),
    });
  }

  addCategory(): void {
    if (!this.categoryName.trim()) return;
    this.api.createCategory({ name: this.categoryName.trim(), description: this.categoryDescription.trim(), imageUrl: '' }).subscribe({
      next: category => {
        this.notice.emit(`${category.name} category created.`);
        this.categoryName = '';
        this.categoryDescription = '';
        this.refreshRequested.emit();
      },
      error: err => this.notice.emit(err?.error?.message ?? 'Could not create this category.'),
    });
  }

  changeOrderStatus(order: StoreOrder, status: OrderStatus): void {
    this.api.setOrderStatus(order.id, status).subscribe({
      next: updated => {
        this.orders.update(items => items.map(item => item.id === updated.id ? updated : item));
        this.notice.emit(`Order #${order.id} is now ${status.toLowerCase()}.`);
        this.refreshDashboard();
      },
      error: err => this.notice.emit(err?.error?.message ?? 'Could not update this order.'),
    });
  }
}
