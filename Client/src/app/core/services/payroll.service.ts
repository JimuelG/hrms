import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { environment } from '../../../environments/environment';
import { Observable } from 'rxjs';
import { AdjustmentTax, Compensation, PayrollAdjustment, PayrollRunDetail, PayrollRunSummary, PayrollSettings, Payslip, PayslipSummary, RecurringItem, RecurringItemForm, RuleSet, RuleSetForm, StatutoryKind, StatutoryPreview } from '../../shared/models/payroll';

@Injectable({
  providedIn: 'root',
})
export class PayrollService {
  private http = inject(HttpClient);
  private runs = `${environment.apiUrl}/payrollruns`;
  private rules = `${environment.apiUrl}/statutory`;
  private comp = `${environment.apiUrl}/compensation`;
  private payslips = `${environment.apiUrl}/payslips`;
  private payrollBase = `${environment.apiUrl}/payroll`;

  listRuns(): Observable<PayrollRunSummary[]> {
    return this.http.get<PayrollRunSummary[]>(this.runs);
  }

  getRun(id: string): Observable<PayrollRunDetail> {
    return this.http.get<PayrollRunDetail>(`${this.runs}/${id}`);
  }

  startRun(periodStart: string, periodEnd: string, notes?: string): Observable<PayrollRunDetail> {
    return this.http.post<PayrollRunDetail>(this.runs, { periodStart, periodEnd, notes });
  }

  recompute(id: string): Observable<PayrollRunDetail> {
    return this.http.post<PayrollRunDetail>(`${this.runs}/${id}/recompute`, {});
  }

  approve(id: string): Observable<PayrollRunDetail> {
    return this.http.post<PayrollRunDetail>(`${this.runs}/${id}/approve`, {});
  }

  sendBack(id: string, reason: string): Observable<PayrollRunDetail> {
    return this.http.post<PayrollRunDetail>(`${this.runs}/${id}/send-back`, { reason });
  }

  finalize(id: string): Observable<PayrollRunDetail> { 
    return this.http.post<PayrollRunDetail>(`${this.runs}/${id}/finalize`, {} );
  }
  
  cancel(id: string, reason: string): Observable<PayrollRunDetail> {
    return this.http.post<PayrollRunDetail>(`${this.runs}/${id}/cancel`, { reason });
  }

  getAdjustments(id: string): Observable<PayrollAdjustment[]> {
    return this.http.get<PayrollAdjustment[]>(`${this.runs}/${id}/adjustments`);
  }

  addAdjustment(id: string, employeeId: string, label: string, amount: number, tax = AdjustmentTax.NonTaxable): Observable<PayrollRunDetail> {
    return this.http.post<PayrollRunDetail>(`${this.runs}/${id}/adjustments`, { employeeId, label, amount, tax });
  }

  removeAdjustment(id: string, adjustmentId: string): Observable<PayrollRunDetail> {
    return this.http.delete<PayrollRunDetail>(`${this.runs}/${id}/adjustments/${adjustmentId}`);
  }

  getSettings(): Observable<PayrollSettings> {
    return this.http.get<PayrollSettings>(`${this.runs}/settings`);
  }

  updateSettings(v: PayrollSettings): Observable<PayrollSettings> {
    return this.http.post<PayrollSettings>(`${this.runs}/settings`, v);
  }

  listRules(kind?: StatutoryKind): Observable<RuleSet[]> {
    return this.http.get<RuleSet[]>(`${this.rules}/rule-sets`, { params: kind ? { kind: String(kind) } : {} });
  }

  createRule(v: RuleSetForm): Observable<RuleSet> {
    return this.http.post<RuleSet>(`${this.rules}/rule-sets`, v);
  }

  deleteRule(id: string): Observable<void> {
    return this.http.delete<void>(`${this.rules}/rule-sets/${id}`);
  }

  previewRules(monthlyBase: number, taxableIncome?: number, taxBasis?: number, date?: string): Observable<StatutoryPreview> {
    const params: Record<string, string> = { monthlyBase: String(monthlyBase) };
    if (taxableIncome !== undefined) {
      params['taxableIncome'] = String(taxableIncome);
    }

    if (taxBasis) {
      params['taxBasis'] = String(taxBasis);
    }

    if (date) params['date'] = date;

    return this.http.get<StatutoryPreview>(`${this.rules}/preview`, { params });
  }

  getCompensation(employeeId: string): Observable<Compensation[]> {
    return this.http.get<Compensation[]>(`${this.comp}/employee/${employeeId}`);
  } 

  setCompensation(employeeId: string, monthlyBasicSalary: number, effectiveFrom: string, reason: string): Observable<Compensation> {
    return this.http.post<Compensation>(`${this.comp}/employee/${employeeId}`, { monthlyBasicSalary, effectiveFrom, reason });
  }

  listMyPayslip(): Observable<PayslipSummary[]> {
    return this.http.get<PayslipSummary[]>(`${this.payslips}/me`);
  }

  getMyPayslip(runId: string): Observable<Payslip> {
    return this.http.get<Payslip>(`${this.payslips}/me/${runId}`);
  }

  getPayslip(runId: string, employeeId: string): Observable<Payslip> {
    return this.http.get<Payslip>(`${this.payslips}/run/${runId}/employee/${employeeId}`);
  }

  listRecurring(employeeId: string): Observable<RecurringItem[]> {
    return this.http.get<RecurringItem[]>(`${this.payrollBase}/recurring-items/employee/${employeeId}`);
  }

  createRecurring(v: RecurringItemForm): Observable<RecurringItem> {
    return this.http.post<RecurringItem>(`${this.payrollBase}/recurring-items`, v);
  }

  deactivateRecurring(id: string): Observable<RecurringItem> {
    return this.http.post<RecurringItem>(`${this.payrollBase}/recurring-items/${id}/deactivate`, {});
  }

  startThirteenth(year: number, cutoffDate: string): Observable<PayrollRunDetail> {
    return this.http.post<PayrollRunDetail>(`${this.runs}/thirteenth-month`, { year, cutoffDate });
  }
}
