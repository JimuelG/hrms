import { CommonModule } from '@angular/common';
import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { PayrollService } from '../../../../core/services/payroll.service';
import { PermissionService } from '../../../../core/services/permission.service';
import { PERMISSIONS } from '../../../../shared/constants/permissions';
import { FREQUENCY_LABELS, KIND_LABELS, PayFrequency, RuleSet, RuleSetForm, StatutoryKind, StatutoryMethod, StatutoryPreview } from '../../../../shared/models/payroll';
import { dateLabel, peso } from '../../../../shared/utils/payroll-ui.util';
import { errorMessage } from '../../../../shared/utils/api-error.util';

@Component({
  selector: 'app-statutory-rules',
  imports: [
    CommonModule,
    FormsModule
  ],
  templateUrl: './statutory-rules.component.html',
  styleUrl: './statutory-rules.component.scss',
})
export class StatutoryRulesComponent implements OnInit {
  private payroll = inject(PayrollService);
  readonly permissions = inject(PermissionService);
  readonly P = PERMISSIONS;

  readonly Kind = StatutoryKind;
  readonly Method = StatutoryMethod;
  readonly kinds = [StatutoryKind.Sss, StatutoryKind.PhilHealth, StatutoryKind.PagIbig, StatutoryKind.WithholdingTax];
  readonly kindLabel = KIND_LABELS;
  readonly frequencyLabel = FREQUENCY_LABELS;
  readonly frequencies = [PayFrequency.SemiMonthly, PayFrequency.Monthly];
  readonly peso = peso;
  readonly dateLabel = dateLabel;

  rules = signal<RuleSet[]>([]);
  loading = signal(true);
  loadError = signal<string | null>(null);
  actionError = signal<string | null>(null);
  open = signal<ReadonlySet<string>>(new Set());

  drawerOpen = signal(false);
  form: RuleSetForm = this.emptyForm();
  formError = signal<string | null>(null);
  saving = signal(false);

  previewBase = 20000;
  previewTaxable = 10000;
  previewTaxBasis = PayFrequency.SemiMonthly;
  preview = signal<StatutoryPreview | null>(null);
  previewError = signal<string | null>(null);

  ngOnInit(): void {
    void this.load();
  }

  load(): void {
    this.loading.set(true);
    this.payroll.listRules().subscribe({
      next: (r) => {
        this.rules.set(r);
        this.loadError.set(null);
        this.loading.set(false);
      },
      error: (err) => {
        this.loadError.set(errorMessage(err));
        this.loading.set(false);
      }
    })
  }

  rulesOf(kind: StatutoryKind): RuleSet[] {
    return this.rules().filter((r) => r.kind === kind);
  }

  toggle(id: string): void {
    this.open.update((s) => {
      const n = new Set(s);
      if (!n.delete(id)) {
        n.add(id);
      }

      return n;
    })
  }

  openLoad(kind: StatutoryKind): void {
    this.form = this.emptyForm();
    this.form.kind = kind;
    this.setKind(kind);
    this.formError.set(null);
    this.drawerOpen.set(true);
  }

  setKind(kind: StatutoryKind): void {
    this.form.kind = kind;
    this.form.method = kind === StatutoryKind.PhilHealth || kind === StatutoryKind.PagIbig ? StatutoryMethod.Rate : StatutoryMethod.Brackets;
    this.form.basis = kind === StatutoryKind.WithholdingTax ? PayFrequency.SemiMonthly : null;
  }

  save(): void {
    const f = this.form;
    if (!f.effectiveFrom) {
      this.formError.set('Choose thte date these figures take effect.'); return;
    }
    if (!f.source.trim()) {
      this.formError.set('Say where these figures came from, for example he circular number.');
      return;
    }

    const isRate = f.method === StatutoryMethod.Rate;
    const payload: RuleSetForm = {
      ...f,
      source: f.source.trim(),
      notes: f.notes?.trim() || null,
      employeeRate: isRate ? f.employeeRate / 100 : 0,
      employerRate: isRate ? f.employerRate / 100 : 0,
      baseFloor: isRate ? f.baseFloor : null,
      baseCeiling: isRate ? f.baseCeiling : null,
      bracketsCsv: isRate ? null : f.bracketsCsv
    };

    this.payroll.createRule(payload).subscribe({
      next: () => {
        this.saving.set(false);
        this.drawerOpen.set(false);
        this.load();
      },
      error: (err) => {
        this.saving.set(false);
        this.formError.set(errorMessage(err));
      }
    });
  }

  remove(r: RuleSet): void {
    if (!confirm(`Delete the ${this.kindLabel[r.kind]} table effective ${r.effectiveFrom}? Finalized payroll runs keep their figures.`)) return;
    this.actionError.set(null);
    this.payroll.deleteRule(r.id).subscribe({
      next: () => {
        this.load();
      },
      error: (err) => {
        this.actionError.set(errorMessage(err));
      }
    });
  }

  runPreview(): void {
    this.previewError.set(null);
    this.payroll.previewRules(this.previewBase, this.previewTaxable, this.previewTaxBasis).subscribe({
      next: (p) => {
        this.preview.set(p);
      },
      error: (err) => {
        this.preview.set(null);
        this.previewError.set(errorMessage(err));
      }
    });
  }

  private emptyForm(): RuleSetForm {
    return {
      kind: StatutoryKind.Sss,
      method: StatutoryMethod.Brackets,
      effectiveFrom: '',
      basis : null,
      employeeRate: 0,
      employerRate: 0,
      baseFloor: null,
      baseCeiling: null,
      source: '',
      notes: null,
      bracketsCsv: ''
    };
  }
}
