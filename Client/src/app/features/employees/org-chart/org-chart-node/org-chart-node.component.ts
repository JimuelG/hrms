import { CommonModule } from '@angular/common';
import { Component, input, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { OrgNode } from '../../../../shared/utils/org-tree.utils';
import { EMPLOYEE_STATUS_LABELS } from '../../../../shared/models/employee';

@Component({
  selector: 'app-org-chart-node',
  imports: [
    CommonModule,
    RouterLink,
    OrgChartNodeComponent
  ],
  templateUrl: './org-chart-node.component.html',
  styleUrl: './org-chart-node.component.scss',
})
export class OrgChartNodeComponent {
  node = input.required<OrgNode>();
  depth = input(0);

  expanded = signal(false);
  statusLabel = EMPLOYEE_STATUS_LABELS;

  initials(firstName: string, lastName: string): string {
    return `${firstName[0] ?? ''}${lastName[0] ?? ''}`.toUpperCase();
  }

  toggle(): void {
    this.expanded.update((v) => !v);
  }
}
