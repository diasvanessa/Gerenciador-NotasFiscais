import { Component, OnInit, signal, computed, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import { ProdutoService } from '../../services/produto.service';
import { NotaFiscalService } from '../../services/nota-fiscal.service';
import { ToastService } from '../../services/toast.service';
import { HealthService } from '../../services/health.service';
import { Produto, CadastrarProdutoRequest } from '../../models/produto.model';
import { NotaFiscal, StatusNotaFiscal } from '../../models/nota-fiscal.model';
import { ModalProdutoComponent } from '../modals/modal-produto/modal-produto.component';
import { ModalSaldoComponent } from '../modals/modal-saldo/modal-saldo.component';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink, ModalProdutoComponent, ModalSaldoComponent],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.css'
})
export class DashboardComponent implements OnInit {
  private readonly produtoService = inject(ProdutoService);
  private readonly notaFiscalService = inject(NotaFiscalService);
  private readonly toastService = inject(ToastService);
  readonly healthService = inject(HealthService);
  private readonly router = inject(Router);

  readonly produtos = signal<Produto[]>([]);
  readonly notasFiscais = signal<NotaFiscal[]>([]);
  readonly isLoading = signal<boolean>(false);

  readonly isModalProdutoOpen = signal<boolean>(false);
  readonly isModalSaldoOpen = signal<boolean>(false);

  produtoSelecionadoSaldo: Produto | null = null;
  saldoAjusteTipo: 'baixar' | 'estornar' = 'baixar';

  readonly totalEstoque = computed(() => {
    return this.produtos().reduce((acc, p) => acc + (p.saldo || 0), 0);
  });

  readonly totalProdutosCadastrados = computed(() => {
    return this.produtos().length;
  });

  readonly totalNotasEmitidas = computed(() => {
    return this.notasFiscais().length;
  });

  readonly notasFechadasCount = computed(() => {
    return this.notasFiscais().filter(n => n.status === StatusNotaFiscal.Fechada).length;
  });

  readonly notasAbertasCount = computed(() => {
    return this.notasFiscais().filter(n => n.status === StatusNotaFiscal.Aberta).length;
  });

  readonly taxaAtendimento = computed(() => {
    const total = this.totalNotasEmitidas();
    if (total === 0) return 0;
    return Math.round((this.notasFechadasCount() / total) * 100);
  });

  readonly produtosComEstoquePositivoCount = computed(() => {
    return this.produtos().filter(p => (p.saldo || 0) > 0).length;
  });

  readonly produtosSemEstoqueCount = computed(() => {
    return this.produtos().filter(p => (p.saldo || 0) === 0).length;
  });

  readonly produtosComEstoqueBaixoCount = computed(() => {
    return this.produtos().filter(p => (p.saldo || 0) > 0 && (p.saldo || 0) < 10).length;
  });

  readonly produtosEstoqueNormalCount = computed(() => {
    return this.produtos().filter(p => (p.saldo || 0) >= 10).length;
  });

  readonly taxaDisponibilidade = computed(() => {
    const total = this.totalProdutosCadastrados();
    if (total === 0) return 0;
    return Math.round((this.produtosComEstoquePositivoCount() / total) * 100);
  });

  readonly taxaEstoqueNormal = computed(() => {
    const total = this.totalProdutosCadastrados();
    if (total === 0) return 0;
    return Math.round((this.produtosEstoqueNormalCount() / total) * 100);
  });

  readonly taxaGeralOperacional = computed(() => {
    const totalProd = this.totalProdutosCadastrados();
    const totalNf = this.totalNotasEmitidas();
    if (totalProd === 0 && totalNf === 0) return 0;
    if (totalNf === 0) return this.taxaDisponibilidade();
    if (totalProd === 0) return this.taxaAtendimento();
    return Math.round((this.taxaDisponibilidade() + this.taxaAtendimento()) / 2);
  });

  readonly maiorSaldoProduto = computed(() => {
    const prods = this.produtos();
    if (prods.length === 0) return 0;
    return Math.max(...prods.map(p => p.saldo || 0));
  });

  readonly produtosTopEstoque = computed(() => {
    const max = this.maiorSaldoProduto();
    return this.produtos()
      .slice()
      .sort((a, b) => (b.saldo || 0) - (a.saldo || 0))
      .slice(0, 6)
      .map(p => ({
        ...p,
        percentual: max > 0 ? Math.max(12, Math.round(((p.saldo || 0) / max) * 100)) : 0
      }));
  });

  readonly produtosDestaque = computed(() => {
    return this.produtos().slice(0, 4);
  });

  readonly gaugeOffset = computed(() => {
    const taxa = this.taxaAtendimento();
    return 283 - (283 * taxa) / 100;
  });

  ngOnInit(): void {
    this.carregarDados();
  }

  carregarDados(): void {
    this.isLoading.set(true);

    this.produtoService.listar().subscribe({
      next: (lista) => {
        this.produtos.set(lista);
        this.isLoading.set(false);
      },
      error: () => {
        this.produtos.set([]);
        this.isLoading.set(false);
      }
    });

    this.notaFiscalService.listar().subscribe({
      next: (lista) => {
        this.notasFiscais.set(lista);
      },
      error: () => {
        this.notasFiscais.set([]);
      }
    });
  }

  getImagemProduto(url?: string | null): string {
    if (url && url.trim().length > 0) {
      return url;
    }
    return '/assets/img/placeholder.svg';
  }

  // Modals
  abrirModalProduto(): void {
    this.isModalProdutoOpen.set(true);
  }

  fecharModalProduto(): void {
    this.isModalProdutoOpen.set(false);
  }

  salvarProduto(novoProduto: CadastrarProdutoRequest): void {
    this.produtoService.cadastrar(novoProduto).subscribe({
      next: (prodCriado) => {
        this.produtos.update(lista => [prodCriado, ...lista]);
        this.fecharModalProduto();
        this.toastService.show(`Produto "${prodCriado.codigo}" cadastrado com sucesso!`, 'success');
      },
      error: (err) => {
        this.toastService.show(err.error?.mensagem || 'Erro ao cadastrar produto.', 'error');
      }
    });
  }

  abrirModalSaldo(produto: Produto, tipo: 'baixar' | 'estornar'): void {
    this.produtoSelecionadoSaldo = produto;
    this.saldoAjusteTipo = tipo;
    this.isModalSaldoOpen.set(true);
  }

  fecharModalSaldo(): void {
    this.isModalSaldoOpen.set(false);
    this.produtoSelecionadoSaldo = null;
  }

  salvarAjusteSaldo(event: { quantidade: number }): void {
    if (!this.produtoSelecionadoSaldo) return;
    const prod = this.produtoSelecionadoSaldo;
    const qtd = event.quantidade;

    if (this.saldoAjusteTipo === 'baixar') {
      this.produtoService.baixarSaldo(prod.codigo, qtd).subscribe({
        next: () => {
          this.produtos.update(lista =>
            lista.map(p => p.codigo === prod.codigo ? { ...p, saldo: p.saldo - qtd } : p)
          );
          this.fecharModalSaldo();
          this.toastService.show(`Saldo de ${qtd} un baixado com sucesso do produto ${prod.codigo}!`, 'success');
        },
        error: (err) => {
          this.toastService.show(err.error?.mensagem || 'Erro ao baixar saldo.', 'error');
        }
      });
    } else {
      this.produtoService.estornarSaldo(prod.codigo, qtd).subscribe({
        next: () => {
          this.produtos.update(lista =>
            lista.map(p => p.codigo === prod.codigo ? { ...p, saldo: p.saldo + qtd } : p)
          );
          this.fecharModalSaldo();
          this.toastService.show(`Estorno de ${qtd} un adicionado com sucesso ao produto ${prod.codigo}!`, 'success');
        },
        error: (err) => {
          this.toastService.show(err.error?.mensagem || 'Erro ao estornar saldo.', 'error');
        }
      });
    }
  }
}
