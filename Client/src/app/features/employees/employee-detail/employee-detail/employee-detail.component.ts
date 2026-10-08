import { CommonModule, DatePipe } from '@angular/common';
import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { EmployeeService } from '../../../../core/services/employee.service';
import { ContactService } from '../../../../core/services/emergency-contact.service';
import { EmployeeDocumentService } from '../../../../core/services/employee-document.service';
import { TimelineService } from '../../../../core/services/timeline.service';
import { DocumentStatus, EmergencyContact, EmergencyContactFormValue, Employee, EMPLOYEE_STATUS_LABELS, EmployeeDocument, EMPLOYMENT_TYPE_LABELS, TIMELINE_ICONS, TimelineEvent } from '../../../../shared/models/employee';
import { extractApiError } from '../../../../shared/utils/api-error.util';
import { WorkScheduleService } from '../../../../core/services/work-schedule.service';
import { WorkSchedule } from '../../../../shared/models/attendance';

type Tab = 'profile' | 'contacts' | 'documents' | 'timeline';

@Component({
  selector: 'app-employee-detail',
  imports: [
    CommonModule,
    FormsModule,
    RouterLink,
    DatePipe
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
      scheduleId: this.scheduleSelection
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
}
