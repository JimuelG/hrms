import { CommonModule } from '@angular/common';
import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { PositionService } from '../../../../core/services/position.service';
import { Position, PositionFormValue } from '../../../../shared/models/organization';
import { extractApiError } from '../../../../shared/utils/api-error.util';

@Component({
  selector: 'app-positions',
  imports: [
    CommonModule,
    FormsModule
  ],
  templateUrl: './positions.component.html',
  styleUrl: './positions.component.scss',
})
export class PositionsComponent implements OnInit{
  private positionService = inject(PositionService);

  positions = signal<Position[]>([]);
  loading = signal(true);
  search = '';
  drawerOpen = signal(false);
  editing = signal<Position | null>(null);
  formError = signal<string | null>(null);
  saving = signal(false);
  form: PositionFormValue = this.emptyForm();

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.positionService.getAll(this.search || undefined).subscribe({
      next: (data) => {
        this.positions.set(data);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
      }
    })
  }

  openCreate(): void {
    this.editing.set(null);
    this.form = this.emptyForm();
    this.formError.set(null);
    this.drawerOpen.set(true);
  }

  openEdit(p: Position): void {
    this.editing.set(p);
    this.form = {
      title: p.title,
      code: p.code,
      description: p.description ?? '',
      isActive: p.isActive
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
    const request = current 
      ? this.positionService.update(current.id, this.form) 
      : this.positionService.create(this.form);

    request.subscribe({
      next: () => {
        this.saving.set(false);
        this.closeDrawer();
        this.load();
      },
      error: (err) => {
        this.saving.set(false);
        this.formError.set(extractApiError(err));
      }
    });
  }

  remove(p: Position): void {
    if (!confirm(`Delete "${p.title}"`)) return;
    this.positionService.delete(p.id).subscribe(() => this.load());
  }

  private emptyForm(): PositionFormValue {
    return {
      title: '',
      code: '',
      description: '',
      isActive: true
    }
  };
}
