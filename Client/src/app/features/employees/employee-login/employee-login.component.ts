import { CommonModule, DatePipe } from '@angular/common';
import { Component, inject, input, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { IssuedLogin, LoginAccount, LoginAccountService } from '../../../core/services/login-account.service';
import { firstValueFrom } from 'rxjs';
import { errorMessage } from '../../../shared/utils/api-error.util';

@Component({
  selector: 'app-employee-login',
  imports: [
    CommonModule,
    FormsModule,
    DatePipe
  ],
  templateUrl: './employee-login.component.html',
  styleUrl: './employee-login.component.scss',
})
export class EmployeeLoginComponent implements OnInit {
  private service = inject(LoginAccountService);
  employeeId = input.required<string>();
  defaultEmail = input<string>('');

  account = signal<LoginAccount | null>(null);
  issued = signal<IssuedLogin | null>(null);
  error = signal<string | null>(null);
  busy = signal(false);
  copied = signal(false);
  email = '';

  ngOnInit(): void {
    this.email = this.defaultEmail();
    void this.load();
  }
  
  private async load(): Promise<void> {
    try {
      this.account.set(await firstValueFrom(this.service.get(this.employeeId())));
    }
    catch (err) {
      this.error.set(errorMessage(err));
    }
  }

  async create(): Promise<void> {
    if (!confirm(`Create a login for ${this.email}`)) return;
    await this.run(() => this.service.create(this.employeeId(), this.email.trim()));
  }

  async reset(): Promise<void> {
    if (!confirm('Issue a new temporary password? Their current password stops working and they are signed out everywhere.')) return;
    await this.run(() => this.service.reset(this.employeeId()));
  }

  private async run(call: () => ReturnType<LoginAccountService['create']>): Promise<void> {
    this.busy.set(true);
    this.error.set(null);
    this.issued.set(null);
    this.copied.set(false);
    try {
      this.issued.set(await firstValueFrom(call()));
      await this.load();
    } catch (err) {
      this.error.set(errorMessage(err));
    } finally {
      this.busy.set(false);
    }
  }

  async copy(text: string): Promise<void> {
    try {
      await navigator.clipboard.writeText(text);
      this.copied.set(true);
    } catch  {
    }
  }

  dismiss(): void {
    this.issued.set(null);
  }
}
