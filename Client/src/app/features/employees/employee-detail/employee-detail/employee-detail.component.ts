import { CommonModule, DatePipe } from '@angular/common';
import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { EmployeeService } from '../../../../core/services/employee.service';
import { ContactService } from '../../../../core/services/emergency-contact.service';
import { EmployeeDocumentService } from '../../../../core/services/employee-document.service';
import { TimelineService } from '../../../../core/services/timeline.service';
import { DocumentStatus, EmergencyContact, EmergencyContactFormValue, Employee, EMPLOYEE_STATUS_LABELS, EmployeeDocument, EMPLOYMENT_TYPE_LABELS, TIMELINE_ICONS, TimelineEvent } from '../../../../shared/models/employee';
import { errorMessage, extractApiError } from '../../../../shared/utils/api-error.util';
import { WorkScheduleService } from '../../../../core/services/work-schedule.service';
import { WORK_LOCATION_TYPE_LABELS, WorkLocation, WorkSchedule } from '../../../../shared/models/attendance';
import { PermissionService } from '../../../../core/services/permission.service';
import { PERMISSIONS } from '../../../../shared/constants/permissions';
import { WorkLocationService } from '../../../../core/services/work-location.service';
import { firstValueFrom } from 'rxjs';
import { EmployeeLeaveBalancesComponent } from '../../employee-leave-balances/employee-leave-balances/employee-leave-balances.component';
import { EmployeeCompensationComponent } from '../../employee-compensaion/employee-compensation/employee-compensation.component';
import { EmployeeRecurringComponent } from '../../employee-recurring/employee-recurring/employee-recurring.component';
import { EmployeeIdentifiersComponent } from '../../employee-identifiers/employee-identifiers/employee-identifiers.component';
import { EmployeeLoginComponent } from '../../employee-login/employee-login.component';

type Tab = 'profile' | 'contacts' | 'documents' | 'timeline';

@Component({
  selector: 'app-employee-detail',
  imports: [
    CommonModule,
    FormsModule,
    RouterLink,
    DatePipe,
    EmployeeLeaveBalancesComponent,
    EmployeeCompensationComponent,
    EmployeeRecurringComponent,
    EmployeeIdentifiersComponent,
    EmployeeLoginComponent
  ],
  templateUrl: './employee-detail.component.html',
  styleUrl: './employee-detail.component.scss',
})
export class EmployeeDetailComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private employeeService = inject(EmployeeService);
  private contactService = inject(ContactService);
  private documentService = inject(EmployeeDocumentService);
  private timelineService = inject(TimelineService);
  private scheduleService = inject(WorkScheduleService);

  readonly permissions = inject(PermissionService);
  readonly P = PERMISSIONS;
  private locationService = inject(WorkLocationService);
  readonly locationTypeLabel = WORK_LOCATION_TYPE_LABELS;

  allLocations = signal<WorkLocation[]>([]);
  selectedLocationIds = signal<ReadonlySet<string>>(new Set());
  locationsError = signal<string | null>(null);
  locationsSaving = signal(false);
  locationsSaved = signal(false);

  linkEmail = '';
  linkSaving = signal(false);
  linkError = signal<string | null>(null);

  employeeId = this.route.snapshot.paramMap.get('id')!;
  employee = signal<Employee | null>(null);
  tab = signal<Tab>('profile');

  statusLabel = EMPLOYEE_STATUS_LABELS;
  employmentTypeLabel = EMPLOYMENT_TYPE_LABELS;
  timelineIcons = TIMELINE_ICONS;
  DocumentStatus = DocumentStatus;

  contacts = signal<EmergencyContact[]>([]);
  contactFormOpen = signal(false);
  contactError = signal<string | null>(null);
  contactForm: EmergencyContactFormValue = {
    name: '',
    relationship: '',
    phone: '',
    alternatePhone: '',
    address: '',
    isPrimary: false
  };

  documents = signal<EmployeeDocument[]>([]);
  uploadType = '';
  uploadExpiration = '';
  selectedFile: File | null = null;
  uploading = signal(false);
  uploadError = signal<string | null>(null);
  schedules = signal<WorkSchedule[]>([]);
  scheduleSelection: string | null = null;
  scheduleSaving = signal(false);
  scheduleError = signal<string | null>(null);

  timeline = signal<TimelineEvent[]>([]);
  noteTitle = '';
  noteDescription = '';
  addingNote = signal(false);

  ngOnInit(): void {
    this.employeeService.getById(this.employeeId).subscribe((e) => {
      this.employee.set(e);
      this.scheduleSelection = e.scheduleId;
    });
    this.scheduleService.getAll().subscribe((s) => this.schedules.set(s.filter((x) => x.isActive)));
    this.loadContacts();
    this.loadDocuments();
    this.loadTimeline();
    void this.loadLocations();
  }

  setTab(t: Tab): void {
    this.tab.set(t);
  }

  loadContacts(): void {
    this.contactService.getForEmployee(this.employeeId).subscribe((c) => {
      this.contacts.set(c);
    })
  }

  openAddContact(): void {
    this.contactForm = {
      name: '',
      relationship: '',
      phone: '',
      alternatePhone: '',
      address: '',
      isPrimary: false
    };
    this.contactError.set(null);
    this.contactFormOpen.set(true);
  }

  saveContact(): void {
    this.contactError.set(null);
    this.contactService.create(this.employeeId, this.contactForm).subscribe({
      next: () => {
        this.contactFormOpen.set(false);
        this.loadContacts();
      },
      error: (err) => {
        this.contactError.set(extractApiError(err));
      }
    })
  }

  removeContact(c: EmergencyContact): void {
    if (!confirm(`Remove emergency contact "${c.name}"?`)) return;
    this.contactService.delete(this.employeeId, c.id).subscribe(() => this.loadContacts());
  }

  loadDocuments(): void {
    this.documentService.getForEmployee(this.employeeId).subscribe((d) => this.documents.set(d));
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.selectedFile = input.files?.[0] ?? null;
  }

  uploadDocument(): void {
    if (!this.selectedFile || !this.uploadType) return;
    this.uploadError.set(null);
    this.uploading.set(true);

    this.documentService.upload(this.employeeId, this.uploadType, this.selectedFile, this.uploadExpiration || undefined).subscribe({
      next: () => {
        this.uploading.set(false);
        this.uploadType = '';
        this.uploadExpiration = '';
        this.selectedFile = null;
        this.loadDocuments();
        this.loadTimeline();
      },
      error: (err) => {
        this.uploading.set(false);
        this.uploadError.set(extractApiError(err));
      }
    });
  }

  downloadDocument(doc: EmployeeDocument): void {
    this.documentService.download(this.employeeId, doc.id).subscribe((blob) => {
      const url = window.URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = doc.originalFileName;
      a.click();
      window.URL.revokeObjectURL(url);
    });
  }

  verifyDocument(doc: EmployeeDocument): void {
    this.documentService.verify(this.employeeId, doc.id, DocumentStatus.Verified).subscribe(() => {
      this.loadDocuments();
      this.loadTimeline();
    })
  }

  rejectDocument(doc: EmployeeDocument): void {
    const reason = prompt('Reason for rejecting this document:');
    if (!reason) return;
    this.documentService.verify(this.employeeId, doc.id, DocumentStatus.Rejected, reason).subscribe(() => {
      this.loadDocuments;
      this.loadTimeline();
    })
  }

  removeDocument(doc: EmployeeDocument): void {
    if (!confirm(`Delete "${doc.originalFileName}"?`)) return;

    this.documentService.delete(this.employeeId, doc.id).subscribe(() => {
      this.loadDocuments();
    })
  }

  loadTimeline(): void {
    this.timelineService.getForEmployee(this.employeeId).subscribe((t) => {
      this.timeline.set(t);
    })
  }

  addNote(): void {
    if (!this.noteTitle) return;

    this.addingNote.set(true);

    this.timelineService.addNote(this.employeeId, { title: this.noteTitle, description: this.noteDescription || undefined}).subscribe({
      next: () => {
        this.addingNote.set(false);
        this.noteTitle = '';
        this.noteDescription = '';
        this.loadTimeline();
      },
      error: () => {
        this.addingNote.set(false);
      }
    })
  }

  saveSchedule(): void {
    const e = this.employee();
    if (!e) return;

    this.scheduleError.set(null);
    this.scheduleSaving.set(true);

    this.employeeService.update(e.id, {
      firstName: e.firstName,
      lastName: e.lastName,
      email: e.email,
      phone: e.phone ?? undefined,
      dateOfBirth: e.dateOfBirth,
      branchId: e.branchId,
      departmentId: e.departmentId,
      positionId: e.positionId,
      managerId: e.managerId,
      employmentType: e.employmentType,
      hireDate: e.hireDate,
      status: e.status,
      scheduleId: this.scheduleSelection,
      lastWorkingDay: e.lastWorkingDay
    }).subscribe({
      next: (updated) => {
        this.employee.set(updated);
        this.scheduleSelection = updated.scheduleId;
        this.scheduleSaving.set(false);
      },
      error: (err) => {
        this.scheduleSaving.set(false);
        this.scheduleError.set(extractApiError(err));
      }
    })
  }

  private async loadLocations(): Promise<void> {
    await this.permissions.ensureLoaded();
    if (!this.permissions.can(PERMISSIONS.employees.write) || !this.permissions.can(PERMISSIONS.workLocations.read)) return;

    try {
      const [all, mine] = await Promise.all([
        firstValueFrom(this.locationService.getAll()),
        firstValueFrom(this.locationService.getForEmployee(this.employeeId))
      ]);
      const active = all.filter((l) => l.isActive);
      this.allLocations.set(active);
      this.selectedLocationIds.set(new Set(mine.filter((m) => active.some((a) => a.id === m.id)).map((m) => m.id)));
    } catch (err) {
      this.locationsError.set(errorMessage(err));
    }
  }

  toggleLocation(id: string): void {
    this.selectedLocationIds.update((s) => {
      const next = new Set(s);
      if (!next.delete(id)) next.add(id);

      return next;
    });
    this.locationsSaved.set(false);
  }

  saveLocations(): void {
    this.locationsError.set(null);
    this.locationsSaved.set(false);
    this.locationsSaving.set(true);
    this.locationService.setForEmployee(this.employeeId, [ ...this.selectedLocationIds()]).subscribe({
      next: () => {
        this.locationsSaving.set(false);
        this.locationsSaved.set(true);
        this.loadTimeline();
      },
      error: (err) => {
        this.locationsSaving.set(false);
        this.locationsError.set(errorMessage(err));
      }
    })
  }

  linkAccount(): void {
    const e = this.employee();
    const email = this.linkEmail.trim();
    if (!e || !email) return;

    this.linkError.set(null);
    this.linkSaving.set(true);
    this.employeeService.linkUser(e.id, email).subscribe({
      next: (updated) => {
        this.employee.set(updated);
        this.linkEmail = '';
        this.linkSaving.set(false);
      },
      error: (err) => {
        this.linkSaving.set(false);
        this.linkError.set(errorMessage(err));
      }
    })
  }

  unlinkAccount(): void {
    const e = this.employee();
    if (!e || !confirm('Unlink this login? The person will no longer be able to clock in until it is linked again.')) return;

    this.linkError.set(null);
    this.linkSaving.set(true);
    this.employeeService.unlinkUser(e.id).subscribe({
      next: (updated) => {
        this.employee.set(updated);
        this.linkSaving.set(false);
      },
      error: (err) => {
        this.linkSaving.set(false);
        this.linkError.set(errorMessage(err));
      }
    })
  }
}
