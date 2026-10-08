import { CommonModule } from '@angular/common';
import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { EmployeeService } from '../../../../core/services/employee.service';
import { BranchService } from '../../../../core/services/branch.service';
import { DepartmentService } from '../../../../core/services/department.service';
import { PositionService } from '../../../../core/services/position.service';
import { Employee, EMPLOYEE_STATUS_LABELS, EmployeeFormValue, EmployeeStatus, EmployeeSummary, EMPLOYMENT_TYPE_LABELS, EmploymentType } from '../../../../shared/models/employee';
import { Branch, Department, Position } from '../../../../shared/models/organization';
import { extractApiError } from '../../../../shared/utils/api-error.util';
import { forkJoin, last } from 'rxjs';
import { WorkScheduleService } from '../../../../core/services/work-schedule.service';
import { WorkSchedule } from '../../../../shared/models/attendance';

@Component({
  selector: 'app-employee-list',
  imports: [
    CommonModule,
    FormsModule,
    RouterLink
  ],
  templateUrl: './employee-list.component.html',
  styleUrl: './employee-list.component.scss',
})
export class EmployeeListComponent implements OnInit {
  private employeeService = inject(EmployeeService);
  private branchService = inject(BranchService);
  private departmentService = inject(DepartmentService);
  private positionService = inject(PositionService);
  private scheduleService = inject(WorkScheduleService);
  private router = inject(Router);

  employees = signal<Employee[]>([]);
  branches = signal<Branch[]>([]);
  departments = signal<Department[]>([]);
  positions = signal<Position[]>([]);
  schedules = signal<WorkSchedule[]>([]);
  eligibleManagers = signal<EmployeeSummary[]>([]);

  loading = signal(true);
  search = '';

  drawerOpen = signal(false);
  editing = signal<Employee | null>(null);
  formError = signal<string | null>(null);
  deleteError = signal<string | null>(null);
  saving = signal(false);
  form: EmployeeFormValue = this.emptyForm();

  employmentTypes = Object.entries(EMPLOYMENT_TYPE_LABELS)
    .map(([value, label]) => ({ value: +value as EmploymentType, label }));
  statuses = Object.entries(EMPLOYEE_STATUS_LABELS)
    .map(([value, label]) => ({ value: +value as EmployeeStatus, label }));
  statusLabel = EMPLOYEE_STATUS_LABELS;

  ngOnInit(): void {
    this.loadAll();
  }

  loadAll(): void {
    forkJoin({
      branches: this.branchService.getAll(),
      departments: this.departmentService.getAll(),
      positions: this.positionService.getAll(),
      schedules: this.scheduleService.getAll(),
      eligibleManagers: this.employeeService.getEligibleManagers()
    }).subscribe(({ branches, departments, positions, schedules, eligibleManagers }) => {
      this.branches.set(branches);
      this.departments.set(departments);
      this.positions.set(positions);
      this.schedules.set(schedules);
      this.eligibleManagers.set(eligibleManagers);
    });
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.employeeService.getAll(this.search || undefined).subscribe({
      next: (data) => {
        this.employees.set(data);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
      }
    })
  }

  viewEmployee(e: Employee): void {
    this.router.navigate(['/employees', e.id]);
  }

  openCreate(): void {
    this.editing.set(null);
    this.form = this.emptyForm();
    this.formError.set(null);
    this.drawerOpen.set(true);
  }

  openEdit(e: Employee, event: Event): void {
    event.stopPropagation();
    this.editing.set(e);
    this.form = {
      firstName: e.firstName,
      lastName: e.lastName,
      email: e.email,
      phone: e.phone ?? '',
      dateOfBirth: e.dateOfBirth,
      branchId: e.branchId,
      departmentId: e.departmentId,
      positionId: e.positionId,
      managerId: e.managerId,
      scheduleId: e.scheduleId,
      employmentType: e.employmentType,
      hireDate: e.hireDate,
      status: e.status
    };
    this.formError.set(null);
    this.drawerOpen.set(true);
  }

  closeDrawer(): void {
    this.drawerOpen.set(false);
  }

  save(): void {
    this.formError.set(null);
    this.saving.set(true);
    
    const current = this.editing();
    const request = current ? this.employeeService.update(current.id, this.form)
                            : this.employeeService.create(this.form);
                            
    request.subscribe({
      next: () => {
        this.saving.set(false);
        this.closeDrawer();
        this.loadAll();
      },
      error: (err) => {
        this.saving.set(false);
        this.formError.set(extractApiError(err));
      }
    })
  }

  remove(e: Employee, event: Event): void {
    event.stopPropagation();

    this.deleteError.set(null);

    if (!confirm(`Delete ${e.firstName} ${e.lastName}?`)) return;

    this.employeeService.delete(e.id).subscribe({
      next: () => {
        this.loadAll();
      },
      error: (err) => {
        this.deleteError.set(extractApiError(err));
      }
    });
  }

  private emptyForm(): EmployeeFormValue {
    return {
      employeeNumber: '',
      firstName: '',
      lastName: '',
      email: '',
      phone: '',
      dateOfBirth: null,
      branchId: '',
      departmentId: '',
      positionId: '',
      managerId: null,
      employmentType: EmploymentType.FullTime,
      hireDate: '',
      status: EmployeeStatus.Probationary
    };
  }
}
