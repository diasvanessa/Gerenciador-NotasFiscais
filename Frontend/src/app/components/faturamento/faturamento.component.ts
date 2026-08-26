import { Component, OnInit, signal, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { NotaFiscalService } from '../../services/nota-fiscal.service';
import { ProdutoService } from '../../services/produto.service';
import { ToastService } from '../../services/toast.service';
import { HealthService } from '../../services/health.service';
import { NotaFiscal, StatusNotaFiscal, CriarNotaFiscalRequest } from '../../models/nota-fiscal.model';
import { Produto } from '../../models/produto.model';
import { ModalNotaComponent } from '../modals/modal-nota/modal-nota.component';

@Component({
  selector: 'app-faturamento',
  standalone: true,
  imports: [CommonModule, ModalNotaComponent],
  templateUrl: './faturamento.component.html',
  styleUrl: './faturamento.component.css'
})
export class FaturamentoComponent implements OnInit {
  private readonly notaFiscalService = inject(NotaFiscalService);
  private readonly produtoService = inject(ProdutoService);
  private readonly toastService = inject(ToastService);
  readonly healthService = inject(HealthService);

  readonly notasFiscais = signal<NotaFiscal[]>([]);
  readonly produtosDisponiveis = signal<Produto[]>([]);
  readonly isLoading = signal<boolean>(false);
  readonly imprimindoNumero = signal<number | null>(null);
  readonly erroConexaoFaturamento = signal<boolean>(false);

  readonly isModalNotaOpen = signal<boolean>(false);

  ngOnInit(): void {
    this.carregarDados();
  }

  carregarDados(): void {
    this.isLoading.set(true);
    this.erroConexaoFaturamento.set(false);

    this.notaFiscalService.listar().subscribe({
      next: (lista) => {
        this.notasFiscais.set(lista);
        this.isLoading.set(false);
        this.erroConexaoFaturamento.set(false);
      },
      error: (err) => {
        this.notasFiscais.set([]);
        this.isLoading.set(false);
        this.erroConexaoFaturamento.set(true);
        this.toastService.show('Não foi possível conectar ao microsserviço de Faturamento.', 'error');
      }
    });

    this.produtoService.listar().subscribe({
      next: (lista) => {
        this.produtosDisponiveis.set(lista);
      },
      error: () => {
        this.produtosDisponiveis.set([]);
      }
    });
  }

  abrirModalNota(): void {
    this.isModalNotaOpen.set(true);
  }

  fecharModalNota(): void {
    this.isModalNotaOpen.set(false);
  }

  salvarNotaFiscal(request: CriarNotaFiscalRequest): void {
    const itensValidos = request.itens.filter(i => i.codigoProduto && i.quantidade > 0);
    if (itensValidos.length === 0) {
      this.toastService.show('Adicione ao menos um item com produto e quantidade válida.', 'error');
      return;
    }

    this.notaFiscalService.criar({ itens: itensValidos }).subscribe({
      next: (nota) => {
        this.notasFiscais.update(lista => [nota, ...lista]);
        this.fecharModalNota();
        this.toastService.show(`Nota Fiscal nº ${nota.numero} criada com sucesso!`, 'success');
      },
      error: (err) => {
        const msg = err.error?.mensagem || 'Erro ao emitir nota fiscal. Verifique a conexão com o microsserviço de Faturamento.';
        this.toastService.show(msg, 'error');
      }
    });
  }

  imprimirNotaFiscal(numero: number): void {
    this.imprimindoNumero.set(numero);

    this.notaFiscalService.imprimir(numero).subscribe({
      next: (res) => {
        this.imprimindoNumero.set(null);
        this.notasFiscais.update(lista =>
          lista.map(n => n.numero === numero ? { ...n, status: StatusNotaFiscal.Fechada } : n)
        );
        this.toastService.show(res.mensagem || `Nota Fiscal nº ${numero} impressa e fechada com sucesso!`, 'success');
        this.healthService.verificarTodos();
      },
      error: (err) => {
        this.imprimindoNumero.set(null);
        this.healthService.verificarTodos();

        if (err.status === 503 || err.error?.status === 'Indisponivel') {
          const detalhes = err.error?.detalhes || 'Serviço de Estoque temporariamente inacessível.';
          this.toastService.show(`⚠️ Falha no Estoque: ${detalhes} A NF #${numero} permaneceu ABERTA e consistente.`, 'error');
          return;
        }

        if (err.status === 422 || err.error?.status === 'ValidacaoFalhou') {
          const detalhes = err.error?.detalhes || err.error?.mensagem || 'Falha de validação no Estoque.';
          const compensacaoMsg = err.error?.estornoExecutado ? ' (Os itens anteriores foram estornados com sucesso).' : '';
          this.toastService.show(`❌ Não foi possível fechar a NF #${numero}: ${detalhes}${compensacaoMsg}`, 'error');
          return;
        }

        const msgGenerica = err.error?.mensagem || 'Falha inesperada ao imprimir nota fiscal.';
        this.toastService.show(msgGenerica, 'error');
      }
    });
  }
}
