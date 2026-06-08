import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { ApiService, MilestoneDto, MethodSummaryDto } from '../core/api.service';
import { StatusBadgeComponent } from '../shared/status-badge.component';

@Component({
  selector: 'app-milestone-detail',
  standalone: true,
  imports: [CommonModule, RouterLink, StatusBadgeComponent],
  template: `
    <div class="page-header">
      <h2 class="f3">{{ milestone?.name ?? '…' }}</h2>
      <button class="btn btn-sm" (click)="loadNext()">
        <svg width="14" height="14" viewBox="0 0 16 16" fill="currentColor">
          <path d="M8.75.75a.75.75 0 0 0-1.5 0V5h-4.5a.75.75 0 0 0 0 1.5h4.5v4.5a.75.75 0 0 0 1.5 0V6.5h4.5a.75.75 0 0 0 0-1.5H8.75Z"/>
        </svg>
        Find next
      </button>
    </div>

    @if (milestone?.description) {
      <div class="text-muted f5 mb-2">{{ milestone!.description }}</div>
    }

    @if (next) {
      <div class="Box mb-4" style="border-color: var(--color-accent-emphasis)">
        <div class="Box-row">
          <svg width="16" height="16" viewBox="0 0 16 16" fill="var(--color-accent-fg)">
            <path d="M8 9.5a1.5 1.5 0 1 0 0-3 1.5 1.5 0 0 0 0 3Z"/><path d="M8 0a8 8 0 1 1 0 16A8 8 0 0 1 8 0ZM1.5 8a6.5 6.5 0 1 0 13 0 6.5 6.5 0 0 0-13 0Z"/>
          </svg>
          <span class="text-muted f6">Next recommended</span>
          <a [routerLink]="['/methods', next.id]" class="text-mono">{{ next.currentName }}</a>
          <app-status-badge [status]="next.status" />
        </div>
      </div>
    }

    <div class="Box">
      <div class="Box-header">Methods <span class="Counter">{{ methods.length }}</span></div>
      @for (m of methods; track m.id) {
        <div class="Box-row">
          <app-status-badge [status]="m.status" />
          <a [routerLink]="['/methods', m.id]" class="text-mono" style="font-weight:500">{{ m.currentName }}</a>
          <span class="text-muted f6 ml-auto text-mono">{{ m.filePath }}:{{ m.startLine }}</span>
        </div>
      } @empty {
        <div class="Box-row text-muted">No methods in this milestone.</div>
      }
    </div>
  `
})
export class MilestoneDetailComponent implements OnInit {
  private api   = inject(ApiService);
  private route = inject(ActivatedRoute);

  milestone: MilestoneDto | null = null;
  methods: MethodSummaryDto[] = [];
  next: MethodSummaryDto | null = null;

  ngOnInit() {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    this.api.getMilestone(id).subscribe(m => this.milestone = m);
    this.api.getMethods({ milestoneId: id, pageSize: 200 }).subscribe(r => this.methods = r.items);
  }

  loadNext() {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    this.api.getMilestoneNext(id).subscribe(m => this.next = m);
  }
}
