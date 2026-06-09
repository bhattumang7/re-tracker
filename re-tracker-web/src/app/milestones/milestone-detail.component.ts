import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { ApiService, MilestoneDto, MethodSummaryDto, CallTreeNodeDto } from '../core/api.service';
import { StatusBadgeComponent } from '../shared/status-badge.component';

@Component({
  selector: 'app-milestone-detail',
  standalone: true,
  imports: [CommonModule, RouterLink, StatusBadgeComponent],
  template: `
    <div class="page-header">
      <h2 class="f3">{{ milestone?.name ?? '…' }}</h2>
      @if (milestone && isComplete) {
        <span class="Label Label--done ml-2" style="vertical-align:middle">✓ Completed</span>
      }
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

    <!-- Scope / progress -->
    @if (milestone) {
      <div class="Box mb-4">
        <div class="Box-row d-flex gap-3" style="flex-wrap:wrap; align-items:center">
          <span class="d-flex align-items-baseline gap-1"><strong class="f4">{{ milestone.totalMethods }}</strong> <span class="text-muted">functions</span></span>
          <span class="d-flex align-items-baseline gap-1"><strong class="f4">{{ milestone.doneMethods }}</strong> <span class="text-muted">done</span></span>
          <span class="d-flex align-items-baseline gap-1"><strong class="f4">{{ milestone.progress }}%</strong> <span class="text-muted">complete</span></span>
          <span class="d-flex gap-2 ml-auto" style="flex-wrap:wrap">
            @for (s of statusEntries; track s[0]) {
              <span class="Label">{{ s[0] }}: {{ s[1] }}</span>
            }
          </span>
        </div>
      </div>
    }

    @if (next) {
      <div class="Box mb-4" style="border-color: var(--color-accent-emphasis)">
        <div class="Box-row">
          <span class="text-muted f6">Next recommended</span>
          <a [routerLink]="['/methods', next.id]" class="text-mono">{{ next.currentName }}</a>
          <app-status-badge [status]="next.status" />
        </div>
      </div>
    }

    <!-- Flat list -->
    <div class="Box mb-4">
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

    <!-- Call tree (root → callees; a function repeats under each caller; ↻ = recursion cut) -->
    <div class="Box">
      <div class="Box-header">Call tree <span class="Counter">{{ treeNodeCount }}</span></div>
      @if (tree.length) {
        @for (root of tree; track $index) {
          <ng-container *ngTemplateOutlet="treeNode; context: { $implicit: root, depth: 0 }"></ng-container>
        }
      } @else {
        <div class="Box-row text-muted">No call tree.</div>
      }
    </div>

    <ng-template #treeNode let-node let-depth="depth">
      <div class="Box-row">
        <span class="d-flex align-center gap-2" style="width:100%" [style.padding-left.px]="depth * 18">
          <app-status-badge [status]="node.status" />
          <a [routerLink]="['/methods', node.id]" class="text-mono">{{ node.currentName }}</a>
          @if (node.cyclic) {
            <span class="text-muted f6" title="recursion — this function is its own ancestor; not expanded again">↻</span>
          }
          <span class="text-muted f6 ml-auto text-mono">{{ node.filePath }}:{{ node.startLine }}</span>
        </span>
      </div>
      @for (c of node.children; track $index) {
        <ng-container *ngTemplateOutlet="treeNode; context: { $implicit: c, depth: depth + 1 }"></ng-container>
      }
    </ng-template>
  `
})
export class MilestoneDetailComponent implements OnInit {
  private api   = inject(ApiService);
  private route = inject(ActivatedRoute);

  milestone: MilestoneDto | null = null;
  methods: MethodSummaryDto[] = [];
  tree: CallTreeNodeDto[] = [];
  next: MethodSummaryDto | null = null;

  get isComplete(): boolean {
    return !!this.milestone && this.milestone.totalMethods > 0
      && this.milestone.doneMethods === this.milestone.totalMethods;
  }

  get statusEntries(): [string, number][] {
    return Object.entries(this.milestone?.byStatus ?? {});
  }

  get treeNodeCount(): number {
    const count = (n: CallTreeNodeDto): number =>
      1 + n.children.reduce((sum, c) => sum + count(c), 0);
    return this.tree.reduce((sum, r) => sum + count(r), 0);
  }

  ngOnInit() {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    this.api.getMilestone(id).subscribe(m => this.milestone = m);
    // Milestone-scoped endpoint (/api/methods has no milestoneId filter).
    this.api.getMilestoneMethods(id).subscribe(r => this.methods = r.items);
    this.api.getMilestoneCallTree(id).subscribe(t => this.tree = t);
  }

  loadNext() {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    this.api.getMilestoneNext(id).subscribe(m => this.next = m);
  }
}
