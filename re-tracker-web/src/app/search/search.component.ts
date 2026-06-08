import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { ApiService, SearchResultItem } from '../core/api.service';
import { StatusBadgeComponent } from '../shared/status-badge.component';

@Component({
  selector: 'app-search',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, StatusBadgeComponent],
  template: `
    <div class="page-header">
      <h2 class="f3">Search</h2>
    </div>

    <div class="d-flex gap-2 mb-4">
      <input class="form-control" style="max-width:480px"
             placeholder="Search methods, files, classes…"
             [(ngModel)]="query" (keydown.enter)="search()" />
      <button class="btn" (click)="search()">
        <svg width="14" height="14" viewBox="0 0 16 16" fill="currentColor">
          <path d="M10.68 11.74a6 6 0 0 1-7.922-8.982 6 6 0 0 1 8.982 7.922l3.04 3.04a.749.749 0 0 1-.326 1.275.749.749 0 0 1-.734-.215ZM11.5 7a4.499 4.499 0 1 0-8.997 0A4.499 4.499 0 0 0 11.5 7Z"/>
        </svg>
        Search
      </button>
    </div>

    @if (searched) {
      <div class="text-muted f6 mb-2">{{ total }} result{{ total === 1 ? '' : 's' }}</div>

      <div class="Box">
        @for (r of results; track r.id + r.type) {
          <div class="Box-row">
            <span class="Counter f6" style="font-size:11px">{{ r.type }}</span>
            @if (r.type === 'method') {
              <a [routerLink]="['/methods', r.id]" class="text-mono" style="font-weight:500">{{ r.name }}</a>
            } @else {
              <span class="text-mono">{{ r.name }}</span>
            }
            @if (r.status) {
              <app-status-badge [status]="r.status" />
            }
            <span class="text-muted f6 ml-auto text-mono truncate" style="max-width:300px">{{ r.filePath }}</span>
          </div>
        } @empty {
          <div class="Box-row text-muted">No results for "{{ query }}".</div>
        }
      </div>
    }
  `
})
export class SearchComponent {
  private api = inject(ApiService);
  query   = '';
  results: SearchResultItem[] = [];
  total   = 0;
  searched = false;

  search() {
    if (!this.query.trim()) return;
    this.api.search(this.query).subscribe(r => {
      this.results  = r.items;
      this.total    = r.total;
      this.searched = true;
    });
  }
}
