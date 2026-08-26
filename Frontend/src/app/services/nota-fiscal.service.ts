import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { NotaFiscal, CriarNotaFiscalRequest } from '../models/nota-fiscal.model';

import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root',
})
export class NotaFiscalService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = environment.faturamentoApiUrl;

  listar(): Observable<NotaFiscal[]> {
    return this.http.get<NotaFiscal[]>(this.apiUrl);
  }

  obterPorNumero(numero: number): Observable<NotaFiscal> {
    return this.http.get<NotaFiscal>(`${this.apiUrl}/${numero}`);
  }

  criar(request: CriarNotaFiscalRequest): Observable<NotaFiscal> {
    return this.http.post<NotaFiscal>(this.apiUrl, request);
  }

  imprimir(numero: number): Observable<{ mensagem: string }> {
    return this.http.post<{ mensagem: string }>(`${this.apiUrl}/${numero}/imprimir`, {});
  }
}
