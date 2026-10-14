import { CommonModule } from '@angular/common';
import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { WorkLocationService } from '../../../../core/services/work-location.service';
import { BranchService } from '../../../../core/services/branch.service';
import { GeolocationError, GeolocationService } from '../../../../core/services/geolocation.service';
import { PermissionService } from '../../../../core/services/permission.service';
import { PERMISSIONS } from '../../../../shared/constants/permissions';
import { WORK_LOCATION_TYPE_LABELS, WorkLocation, WorkLocationFormValue, WorkLocationType } from '../../../../shared/models/attendance';
import { mapUrl } from '../../../../shared/utils/attendance-ui.utils';
import { Branch } from '../../../../shared/models/organization';
import { errorMessage } from '../../../../shared/utils/api-error.util';

@Component({
  selector: 'app-work-locations',
  imports: [
    CommonModule,
    FormsModule
  ],
  templateUrl: './work-locations.component.html',
  styleUrl: './work-locations.component.scss',
})
export class WorkLocationsComponent implements OnInit{
  private service = inject(WorkLocationService);
  private branchService = inject(BranchService);
  private geo = inject(GeolocationService);
  readonly permissions = inject(PermissionService);
  readonly P = PERMISSIONS;
  readonly Type = WorkLocationType;
  readonly typeLabel = WORK_LOCATION_TYPE_LABELS;
  readonly mapUrl = mapUrl;
  readonly typeOptions = Object.values(WorkLocationType).filter((v): v is WorkLocationType => typeof v === 'number');

  locations = signal<WorkLocation[]>([]);
  branches = signal<Branch[]>([]);
  loading = signal(true);
  loadError = signal<string | null>(null);
  actionError = signal<string | null>(null);

  drawerOpen = signal(false);
  editing = signal<WorkLocation | null>(null);
  formError = signal<string | null>(null);
  saving = signal(false);
  form: WorkLocationFormValue = this.emptyForm();

  gpsBusy = signal(false);
  gpsMessage = signal<string | null>(null);

  ngOnInit(): void {
    this.branchService.getAll().subscribe({ next: (b) => this.branches.set(b), error: () => {} });
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.service.getAll().subscribe({
      next: (data) => {
        this.locations.set(data);
        this.loadError.set(null);
        this.loading.set(false);
      },
      error: (err) => {
        this.loadError.set(errorMessage(err));
        this.loading.set(false);
      }
    })
  }

  openCreate(): void {
    this.editing.set(null);
    this.form = this.emptyForm();
    this.formError.set(null);
    this.gpsMessage.set(null);
    this.drawerOpen.set(true);
  }

  openEdit(l: WorkLocation): void {
    this.editing.set(l);
    this.form ={ 
      name: l.name,
      type: l.type,
      latitude: l.latitude,
      longitude: l.longitude,
      radiusMeters: l.radiusMeters,
      branchId: l.branchId,
      validFrom: l.validFrom,
      validTo: l.validTo,
      isActive: l.isActive
    };
    this.formError.set(null);
    this.gpsMessage.set(null);
    this.drawerOpen.set(true);
  }

  closeDrawer(): void { this.drawerOpen.set(false); }

  setType(type: WorkLocationType): void {
    this.form.type = type;
    if (type === WorkLocationType.Remote) {
      this.form.latitude = null;
      this.form.longitude = null;
      this.form.radiusMeters = null;
    } else if (this.form.radiusMeters === null) {
      this.form.radiusMeters = 150;
    }
    if (type !== WorkLocationType.Temporary) {
      this.form.validFrom = null;
      this.form.validTo = null;
    }
  }

  async useMyLocation(): Promise<void> {
    this.gpsBusy.set(true);
    this.gpsMessage.set(null);
    try {
      const fix = await this.geo.getPosition();
      this.form.latitude = Math.round(fix.latitude * 1e6) / 1e6;
      this.form.longitude = Math.round(fix.longitude * 1e6) / 1e6;
      this.gpsMessage.set(
        `Captured, accurate to about ${Math.round(fix.accuracyMeters)} m.` +
        (fix.accuracyMeters > 50 ? ' For a better fix, capture this on a phone while standing at the location.' : ''));
    } catch (e) {
      this.gpsMessage.set(e instanceof GeolocationError ? e.message : "Couldn't read your location.");
    } finally {
      this.gpsBusy.set(false);
    }
  }

  save(): void {
    this.formError.set(null);
    this.saving.set(true);

    const payload: WorkLocationFormValue = {
      ...this.form,
      name: this.form.name.trim(),
      branchId: this.form.branchId || null
    };

    const current = this.editing();
    const request = current ? this.service.update(current.id, payload) : this.service.create(payload);

    request.subscribe({
      next: () => {
        this.saving.set(false);
        this.closeDrawer();
        this.load();
      },
      error: (err) => {
        this.saving.set(false);
        this.formError.set(errorMessage(err));
      }
    })
  }

  remove(l: WorkLocation): void {
    if (!confirm(`Delete "${l.name}"? Employees assigned to it will no longer be able to clock in there.`)) return;
    this.actionError.set(null);
    this.service.delete(l.id).subscribe({
      next: () => this.load(),
      error: (err) => this.actionError.set(errorMessage(err))
    });
  }

  private emptyForm(): WorkLocationFormValue {
    return {
      name: '',
      type: WorkLocationType.Office,
      latitude: null,
      longitude: null,
      radiusMeters: 150,
      branchId: null,
      validFrom: null,
      validTo: null,
      isActive: true
    };
  }
}
