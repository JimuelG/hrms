import { CommonModule } from '@angular/common';
import { Component, inject, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { OrgChartNodeComponent } from '../org-chart-node/org-chart-node.component';
import { EmployeeService } from '../../../../core/services/employee.service';
import { buildOrgTree, OrgNode } from '../../../../shared/utils/org-tree.utils';

@Component({
  selector: 'app-org-chart',
  imports: [
    CommonModule,
    RouterLink,
    OrgChartNodeComponent
  ],
  templateUrl: './org-chart.component.html',
  styleUrl: './org-chart.component.scss',
})
export class OrgChartComponent implements OnInit {
  private employeeService = inject(EmployeeService);

  tree = signal<OrgNode[]>([]);
  loading = signal(true);
  isEmpty = signal(false);

  ngOnInit(): void {
    this.employeeService.getAll().subscribe({
      next: (employees) => {
        this.tree.set(buildOrgTree(employees));
        this.isEmpty.set(employees.length === 0);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }
}
