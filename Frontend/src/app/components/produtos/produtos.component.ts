import { Component, OnInit, signal, computed, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ProdutoService } from '../../services/produto.service';
import { ToastService } from '../../services/toast.service';
import { HealthService } from '../../services/health.service';
import { Produto, CadastrarProdutoRequest } from '../../models/produto.model';
import { ModalProdutoComponent } from '../modals/modal-produto/modal-produto.component';
import { ModalSaldoComponent } from '../modals/modal-saldo/modal-saldo.component';

@Component({
  selector: 'app-produtos',
  standalone: true,
  imports: [CommonModule, FormsModule, ModalProdutoComponent, ModalSaldoComponent],
  templateUrl: './produtos.component.html',
  styleUrl: './produtos.component.css'
})
export class ProdutosComponent implements OnInit {
  private readonly produtoService = inject(ProdutoService);
  private readonly toastService = inject(ToastService);
  readonly healthService = inject(HealthService);

  readonly produtos = signal<Produto[]>([]);
  readonly searchTerm = signal<string>('');
  readonly isLoading = signal<boolean>(false);
  readonly erroConexaoEstoque = signal<boolean>(false);

  readonly isModalProdutoOpen = signal<boolean>(false);
  readonly isModalSaldoOpen = signal<boolean>(false);

  produtoSelecionadoSaldo: Produto | null = null;
  saldoAjusteTipo: 'baixar' | 'estornar' = 'baixar';

  readonly produtosFiltrados = computed(() => {
    const term = this.searchTerm().trim().toLowerCase();
    if (!term) return this.produtos();
    return this.produtos().filter(p =>
      p.codigo.toLowerCase().includes(term) ||
      p.descricao.toLowerCase().includes(term)
    );
  });

  ngOnInit(): void {
    this.carregarProdutos();
  }

  carregarProdutos(): void {
    this.isLoading.set(true);
    this.erroConexaoEstoque.set(false);

    this.produtoService.listar().subscribe({
      next: (lista) => {
        this.produtos.set(lista);
        this.isLoading.set(false);
        this.erroConexaoEstoque.set(false);
      },
      error: () => {
        this.produtos.set([]);
        this.isLoading.set(false);
        this.erroConexaoEstoque.set(true);
        this.toastService.show('Não foi possível conectar ao microsserviço de Estoque (Porta 5032).', 'error');
      }
    });
  }

  getImagemProduto(url?: string | null): string {
    if (url && url.trim().length > 0) {
      return url;
    }
    return '/assets/img/placeholder.svg';
  }

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
        this.healthService.verificarTodos();
      },
      error: (err) => {
        this.healthService.verificarTodos();
        const msg = err.error?.mensagem || 'Erro ao cadastrar produto. Verifique se o microsserviço de Estoque está online.';
        this.toastService.show(msg, 'error');
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
          this.healthService.verificarTodos();
        },
        error: (err) => {
          this.healthService.verificarTodos();
          const msg = err.error?.mensagem || 'Erro ao baixar saldo. Verifique a conexão com o microsserviço de Estoque.';
          this.toastService.show(msg, 'error');
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
          this.healthService.verificarTodos();
        },
        error: (err) => {
          this.healthService.verificarTodos();
          const msg = err.error?.mensagem || 'Erro ao estornar saldo. Verifique a conexão com o microsserviço de Estoque.';
          this.toastService.show(msg, 'error');
        }
      });
    }
  }
}
