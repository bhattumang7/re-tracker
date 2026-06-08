import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ApiService, SummaryDto } from '../core/api.service';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="page-header">
      <h2 class="f3">Dashboard</h2>
    </div>

    @if (s) {
      <div class="stat-grid">
        <div class="stat-card">
          <div class="stat-num">{{ s.totalMethods }}</div>
          <div class="stat-lbl">Total methods</div>
        </div>
        <div class="stat-card">
          <div class="stat-num" style="color:var(--color-success-fg)">{{ s.byStatus['Done'] }}</div>
          <div class="stat-lbl">Done</div>
        </div>
        <div class="stat-card">
          <div class="stat-num" style="color:var(--status-inprogress)">{{ s.byStatus['InProgress'] }}</div>
          <div class="stat-lbl">In progress</div>
        </div>
        <div class="stat-card">
          <div class="stat-num" style="color:var(--status-needsreview)">{{ s.byStatus['NeedsReview'] }}</div>
          <div class="stat-lbl">Needs review</div>
        </div>
        <div class="stat-card">
          <div class="stat-num" style="color:var(--color-fg-muted)">{{ s.byStatus['Pending'] }}</div>
          <div class="stat-lbl">Pending</div>
        </div>
        <div class="stat-card">
          <div class="stat-num" style="color:var(--color-fg-subtle)">{{ s.byStatus['Skipped'] }}</div>
          <div class="stat-lbl">Skipped</div>
        </div>
      </div>

      <div class="Box mt-4">
        <div class="Box-header d-flex align-center gap-3">
          <span>Progress</span>
          <span class="text-muted f6">{{ s.overallProgress | number:'1.1-1' }}% complete</span>
          <span class="ml-auto text-mono text-muted">{{ s.byStatus['Done'] }}/{{ s.totalMethods }}</span>
        </div>
        <div style="padding: 16px;">
          <div class="progress-bar-outer">
            <div class="progress-bar-inner" [style.width.%]="s.overallProgress"></div>
          </div>
        </div>
      </div>
    } @else {
      <div class="text-muted">Loading…</div>
    }
  `,
  styles: [`
    .stat-grid {
      display: grid;
      grid-template-columns: repeat(auto-fill, minmax(150px, 1fr));
      gap: 12px;
    }
  `]
})
export class DashboardComponent implements OnInit {
  private api = inject(ApiService);
  s: SummaryDto | null = null;

  ngOnInit() { this.api.getSummary().subscribe(s => this.s = s); }
}
