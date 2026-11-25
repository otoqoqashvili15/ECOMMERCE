import { CurrencyPipe } from '@angular/common';
import { Component, input, output } from '@angular/core';
import { Product } from '../core/store.models';

@Component({
  selector: 'app-product-card',
  standalone: true,
  imports: [CurrencyPipe],
  template: `
    <article class="product-card">
      <div class="product-photo">
        @if (product().images[0]) {
          <img [src]="product().images[0]" [alt]="product().name" loading="lazy" />
        } @else {
          <div class="photo-placeholder">{{ product().categoryName }}</div>
        }
        @if (product().discountPercent > 0) {
          <span class="sale-tag">{{ product().discountPercent }}% off</span>
        }
      </div>
      <div class="product-info">
        <div class="product-kicker"><span>{{ product().brand || product().categoryName }}</span><button type="button" (click)="reviews.emit(product())">★ {{ product().rating || 'New' }}@if (product().reviewCount) { · {{ product().reviewCount }} }</button></div>
        <h3>{{ product().name }}</h3>
        <p>{{ product().description }}</p>
        <div class="product-bottom">
          <div class="price-stack">
            <strong>{{ product().effectivePrice | currency:'USD':'symbol':'1.2-2' }}</strong>
            @if (product().effectivePrice < product().price) {
              <del>{{ product().price | currency:'USD':'symbol':'1.2-2' }}</del>
            }
          </div>
          <button class="add-button" type="button" [disabled]="product().stock < 1" (click)="add.emit(product())">
            @if (product().stock < 1) { Sold out } @else { <span aria-hidden="true">＋</span> Add }
          </button>
        </div>
        @if (product().stock > 0 && product().stock <= product().minimumStock) {
          <div class="stock-note">Only {{ product().stock }} left</div>
        }
      </div>
    </article>
  `,
  styleUrl: './product-card.component.scss',
})
export class ProductCardComponent {
  readonly product = input.required<Product>();
  readonly add = output<Product>();
  readonly reviews = output<Product>();
}
