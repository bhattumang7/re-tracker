import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { ApiService, MethodSummaryDto } from '../core/api.service';
import { StatusBadgeComponent } from '../shared/status-badge.component';

const STATUSES = ['', 'Pending', 'InProgress', 'NeedsReview', 'Done', 'Skipped', 'Deferred'];

@Component({
  selector: 'app-method-list',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, StatusBadgeComponent],
  template: `
    <div class="page-header">
      <h2 class="f3">Methods</h2>
      <span class="Counter ml-auto">{{ total }}</span>
    </div>

    <!-- ── Filters ──────────────────────────────────────────────── -->
    <div class="d-flex gap-2 mb-4">
      <input class="form-control" style="max-width:260px"
             placeholder="Filter by name…"
             [(ngModel)]="filterName" (keydown.enter)="load()" />
      <select class="form-control" style="max-width:160px"
              [(ngModel)]="filterStatus" (change)="load()">
        <option value="">All statuses</option>
        @for (s of statuses; track s) {
          <option [value]="s">{{ s }}</option>
        }
      </select>
      <button class="btn btn-sm" (click)="load()">Apply</button>
    </div>

    <!-- ── Table ────────────────────────────────────────────────── -->
    <div class="Box">
      <table class="gh-table" style="width:100%">
        <thead>
          <tr>
            <th>Status</th>
            <th>Name</th>
            <th>Return type</th>
            <th>File</th>
          </tr>
        </thead>
        <tbody>
          @for (m of methods; track m.id) {
            <tr style="cursor:pointer" [routerLink]="['/methods', m.id]">
              <td><app-status-badge [status]="m.status" /></td>
              <td class="text-mono" style="font-weight:500">{{ m.currentName }}</td>
              <td class="text-mono text-muted">{{ m.returnType }}</td>
              <td class="text-muted f6 text-mono truncate" style="max-width:300px">{{ m.filePath }}:{{ m.startLine }}</td>
            </tr>
          } @empty {
            <tr><td colspan="4" class="text-muted" style="text-align:center; padding:24px">No methods found.</td></tr>
          }
        </tbody>
      </table>
    </div>

    <!-- ── Pagination ────────────────────────────────────────────── -->
    <div class="d-flex align-center gap-2 mt-3">
      <button class="btn btn-sm" [disabled]="page === 0" (click)="prev()">← Previous</button>
      <span class="text-muted f6">Page {{ page + 1 }}</span>
      <button class="btn btn-sm" [disabled]="(page + 1) * pageSize >= total" (click)="next()">Next →</button>
    </div>
  `
})
export class MethodListComponent implements OnInit {
  private api = inject(ApiService);

  methods: MethodSummaryDto[] = [];
  total = 0;
  page = 0;
  pageSize = 50;
  filterStatus = '';
  filterName = '';
  statuses = STATUSES.slice(1);

  ngOnInit() { this.load(); }

  load() {
    const params: Record<string, any> = { page: this.page + 1, pageSize: this.pageSize };
    if (this.filterStatus) params['status'] = this.filterStatus;
    if (this.filterName)   params['nameContains'] = this.filterName;
    this.api.getMethods(params).subscribe(r => { this.methods = r.items; this.total = r.total; });
  }

  prev() { this.page--; this.load(); }
  next() { this.page++; this.load(); }
}
