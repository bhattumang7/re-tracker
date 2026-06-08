import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { ApiService, MilestoneTreeDto } from '../core/api.service';

@Component({
  selector: 'app-milestone-list',
  standalone: true,
  imports: [CommonModule, RouterLink],
  template: `
    <div class="page-header">
      <h2 class="f3">Milestones</h2>
    </div>

    <div class="Box">
      @for (m of tree; track m.id) {
        <ng-container *ngTemplateOutlet="row; context: { $implicit: m, depth: 0 }" />
      } @empty {
        <div class="Box-row text-muted">No milestones yet.</div>
      }
    </div>

    <ng-template #row let-m let-depth="depth">
      <div class="Box-row" [style.padding-left.px]="16 + depth * 20">
        <svg width="16" height="16" viewBox="0 0 16 16" fill="var(--color-fg-muted)" style="flex-shrink:0">
          <path d="M7.75 0a.75.75 0 0 1 .75.75V3h3.634c.414 0 .814.147 1.13.414l2.07 1.75a1.75 1.75 0 0 1 0 2.672l-2.07 1.75a1.75 1.75 0 0 1-1.13.414H8.5v5.25a.75.75 0 0 1-1.5 0V10H2.75A1.75 1.75 0 0 1 1 8.25v-2.5C1 4.784 1.784 4 2.75 4H7V.75A.75.75 0 0 1 7.75 0Z"/>
        </svg>
        <a [routerLink]="['/milestones', m.id]" class="f5" style="font-weight:600">{{ m.name }}</a>
        @if (m.description) {
          <span class="text-muted f6">{{ m.description }}</span>
        }
      </div>
      @for (child of m.children; track child.id) {
        <ng-container *ngTemplateOutlet="row; context: { $implicit: child, depth: depth + 1 }" />
      }
    </ng-template>
  `
})
export class MilestoneListComponent implements OnInit {
  private api = inject(ApiService);
  tree: MilestoneTreeDto[] = [];

  ngOnInit() { this.api.getMilestoneTree().subscribe(t => this.tree = t); }
}
