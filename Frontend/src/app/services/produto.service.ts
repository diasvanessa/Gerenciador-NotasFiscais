import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Produto, CadastrarProdutoRequest } from '../models/produto.model';

import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root',
})
export class ProdutoService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = environment.estoqueApiUrl;

  listar(): Observable<Produto[]> {
    console.log(this.apiUrl);
    return this.http.get<Produto[]>(this.apiUrl);
  }

  obterPorCodigo(codigo: string): Observable<Produto> {
    return this.http.get<Produto>(`${this.apiUrl}/${codigo}`);
  }

  cadastrar(produto: CadastrarProdutoRequest): Observable<Produto> {
    return this.http.post<Produto>(this.apiUrl, produto);
  }

  baixarSaldo(codigo: string, quantidade: number): Observable<{ mensagem: string }> {
    return this.http.patch<{ mensagem: string }>(`${this.apiUrl}/${codigo}/baixar-saldo`, { quantidade });
  }

  estornarSaldo(codigo: string, quantidade: number): Observable<{ mensagem: string }> {
    return this.http.patch<{ mensagem: string }>(`${this.apiUrl}/${codigo}/estornar-saldo`, { quantidade });
  }

  uploadImagem(arquivo: File): Observable<{ url: string; nomeArquivo: string }> {
    const formData = new FormData();
    formData.append('arquivo', arquivo);
    return this.http.post<{ url: string; nomeArquivo: string }>(`${this.apiUrl}/upload-imagem`, formData);
  }

  reconhecerImagem(arquivo: File): Observable<{ sugestao: string }> {
    const formData = new FormData();
    formData.append('arquivo', arquivo);
    // Chama a rota de IA no backend: http://localhost:5032/api/ia/reconhecer-imagem
    const baseUrl = this.apiUrl.replace('/api/produtos', '');
    return this.http.post<{ sugestao: string }>(`${baseUrl}/api/ia/reconhecer-imagem`, formData);
  }
}
