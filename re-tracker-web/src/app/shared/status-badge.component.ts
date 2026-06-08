import { Component, Input } from '@angular/core';

@Component({
  selector: 'app-status-badge',
  standalone: true,
  template: `<span class="Label" [class]="cssClass">{{ status }}</span>`,
})
export class StatusBadgeComponent {
  @Input() status = 'Pending';

  get cssClass() {
    return 'Label--' + this.status.toLowerCase().replace('needsreview', 'needsreview');
  }
}
