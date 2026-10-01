import { Component, inject, OnInit, signal } from '@angular/core';
import { MeResponse } from '../../shared/models/auth';
import { AuthService } from '../../core/auth/auth.service';

@Component({
  selector: 'app-dashboard',
  imports: [],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss',
})
export class DashboardComponent implements OnInit{
  private authService = inject(AuthService);

  me = signal<MeResponse | null>(null);

  ngOnInit(): void {
    this.authService.me().subscribe((res) => this.me.set(res));
  }
  
}
