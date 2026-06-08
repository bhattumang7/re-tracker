import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { ApiService, MethodDetailDto } from '../core/api.service';
import { StatusBadgeComponent } from '../shared/status-badge.component';
import { CopyButtonComponent } from '../shared/copy-button.component';

const STATUSES = ['Pending', 'InProgress', 'NeedsReview', 'Done', 'Skipped', 'Deferred'];

@Component({
  selector: 'app-method-panel',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, StatusBadgeComponent, CopyButtonComponent],
  template: `
    @if (m) {
      <!-- Header -->
      <div class="page-header">
        <div>
          <div class="d-flex align-center gap-2 mb-2">
            <h2 class="f3 text-mono">{{ m.currentName }}</h2>
            <app-status-badge [status]="m.status" />
          </div>
          <div class="text-muted f6 text-mono">
            {{ m.filePath }}:{{ m.startLine }}
            <app-copy-button [text]="m.filePath" />
          </div>
        </div>
      </div>

      <!-- Flash messages -->
      @if (flash()) {
        <div class="flash" [class]="flashClass()">{{ flash() }}</div>
      }

      <!-- Status update -->
      <div class="Box mb-4">
        <div class="Box-header">Update status</div>
        <div class="Box-row d-flex gap-2" style="align-items:flex-end">
          <select class="form-control" style="max-width:180px" [(ngModel)]="newStatus">
            @for (s of statuses; track s) {
              <option [value]="s">{{ s }}</option>
            }
          </select>
          <input class="form-control" style="flex:1" placeholder="Optional comment…" [(ngModel)]="comment" />
          <button class="btn btn-primary btn-sm" (click)="save()">Save</button>
        </div>
      </div>

      <!-- Detail grid -->
      <div class="Box mb-4">
        <div class="Box-header">Details</div>
        <div class="Box-row"><span class="text-muted" style="width:140px;flex-shrink:0">Original name</span><span class="text-mono">{{ m.originalName }}</span></div>
        <div class="Box-row"><span class="text-muted" style="width:140px;flex-shrink:0">Return type</span><span class="text-mono">{{ m.returnType }}</span></div>
        <div class="Box-row"><span class="text-muted" style="width:140px;flex-shrink:0">Location</span><span class="text-mono">line {{ m.startLine }}, col {{ m.startColumn }}</span></div>
        @if (m.statusComment) {
          <div class="Box-row"><span class="text-muted" style="width:140px;flex-shrink:0">Comment</span><span>{{ m.statusComment }}</span></div>
        }
      </div>

      <!-- Parameters -->
      @if (m.parameters.length) {
        <div class="Box mb-4">
          <div class="Box-header">Parameters <span class="Counter">{{ m.parameters.length }}</span></div>
          @for (p of m.parameters; track p.id) {
            <div class="Box-row d-flex gap-3">
              <span class="text-muted text-mono">{{ p.type }}</span>
              <span class="text-mono" style="font-weight:500">{{ p.currentName }}</span>
              <span class="text-muted f6 ml-auto text-mono">{{ p.startLine }}:{{ p.startColumn }}</span>
            </div>
          }
        </div>
      }

      <!-- Callers -->
      @if (m.callers.length) {
        <div class="Box mb-4">
          <div class="Box-header">Called by <span class="Counter">{{ m.callers.length }}</span></div>
          @for (c of m.callers; track c.id) {
            <div class="Box-row">
              <a [routerLink]="['/methods', c.id]" class="text-mono">{{ c.currentName }}</a>
              <app-status-badge [status]="c.status" />
            </div>
          }
        </div>
      }

      <!-- Callees -->
      @if (m.callees.length) {
        <div class="Box mb-4">
          <div class="Box-header">Calls <span class="Counter">{{ m.callees.length }}</span></div>
          @for (c of m.callees; track c.id) {
            <div class="Box-row">
              <a [routerLink]="['/methods', c.id]" class="text-mono">{{ c.currentName }}</a>
              <app-status-badge [status]="c.status" />
            </div>
          }
        </div>
      }

      <!-- Rename history -->
      @if (m.renameHistory.length) {
        <div class="Box mb-4">
          <div class="Box-header">Rename history</div>
          @for (h of m.renameHistory; track h.id) {
            <div class="Box-row">
              <span class="text-muted f6" style="width:140px;flex-shrink:0">{{ h.timestamp | date:'short' }}</span>
              <span class="text-mono">{{ h.oldName }}</span>
              <span class="text-muted">→</span>
              <span class="text-mono">{{ h.newName }}</span>
            </div>
          }
        </div>
      }
    } @else {
      <div class="text-muted">Loading…</div>
    }
  `
})
export class MethodPanelComponent implements OnInit {
  private api   = inject(ApiService);
  private route = inject(ActivatedRoute);

  m: MethodDetailDto | null = null;
  newStatus = 'Pending';
  comment = '';
  statuses = STATUSES;
  flash = signal('');
  flashClass = signal('flash flash-success');

  ngOnInit() {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    this.api.getMethod(id).subscribe(m => {
      this.m         = m;
      this.newStatus = m.status;
      this.comment   = m.statusComment ?? '';
    });
  }

  save() {
    if (!this.m) return;
    this.api.updateStatus(this.m.id, { status: this.newStatus, comment: this.comment || undefined }).subscribe({
      next: () => {
        this.m!.status        = this.newStatus;
        this.m!.statusComment = this.comment || null;
        this.flash.set('Status updated.');
        this.flashClass.set('flash flash-success');
        setTimeout(() => this.flash.set(''), 3000);
      },
      error: () => {
        this.flash.set('Failed to save.');
        this.flashClass.set('flash flash-error');
      }
    });
  }
}
