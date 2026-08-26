import { Injectable, signal, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../environments/environment';

export interface ServiceHealth {
  online: boolean;
  status: string;
  lastCheck?: Date;
}

@Injectable({
  providedIn: 'root'
})
export class HealthService {
  private readonly http = inject(HttpClient);

  // URLs base dos microsserviços
  private readonly estoqueBaseUrl = environment.estoqueApiUrl.replace(/\/api\/produtos\/?$/, '');
  private readonly faturamentoBaseUrl = environment.faturamentoApiUrl.replace(/\/api\/faturamento\/?$/, '');

  readonly estoqueHealth = signal<ServiceHealth>({ online: false, status: 'Verificando...' });
  readonly faturamentoHealth = signal<ServiceHealth>({ online: false, status: 'Verificando...' });
  readonly isChecking = signal<boolean>(false);

  constructor() {
    this.verificarTodos();
    // Verifica a saúde dos microsserviços periodicamente a cada 15 segundos
    setInterval(() => {
      this.verificarTodos();
    }, 15000);
  }

  verificarTodos(): void {
    this.isChecking.set(true);
    this.verificarEstoque();
    this.verificarFaturamento();
  }

  verificarEstoque(): void {
    this.http.get(`${this.estoqueBaseUrl}/health`, { responseType: 'text' }).subscribe({
      next: () => {
        this.estoqueHealth.set({
          online: true,
          status: 'Online',
          lastCheck: new Date()
        });
        this.isChecking.set(false);
      },
      error: () => {
        this.estoqueHealth.set({
          online: false,
          status: 'Offline',
          lastCheck: new Date()
        });
        this.isChecking.set(false);
      }
    });
  }

  verificarFaturamento(): void {
    this.http.get(`${this.faturamentoBaseUrl}/health`, { responseType: 'text' }).subscribe({
      next: () => {
        this.faturamentoHealth.set({
          online: true,
          status: 'Online',
          lastCheck: new Date()
        });
        this.isChecking.set(false);
      },
      error: () => {
        this.faturamentoHealth.set({
          online: false,
          status: 'Offline',
          lastCheck: new Date()
        });
        this.isChecking.set(false);
      }
    });
  }
}
