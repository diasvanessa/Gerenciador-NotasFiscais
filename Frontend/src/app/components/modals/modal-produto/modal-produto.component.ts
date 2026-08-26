import { Component, input, output, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ProdutoService } from '../../../services/produto.service';
import { ToastService } from '../../../services/toast.service';
import { CadastrarProdutoRequest } from '../../../models/produto.model';

@Component({
  selector: 'app-modal-produto',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './modal-produto.component.html',
  styleUrl: './modal-produto.component.css'
})
export class ModalProdutoComponent {
  private readonly produtoService = inject(ProdutoService);
  private readonly toastService = inject(ToastService);

  readonly isOpen = input<boolean>(false);
  readonly close = output<void>();
  readonly save = output<CadastrarProdutoRequest>();

  novoProduto: CadastrarProdutoRequest = {
    codigo: '',
    descricao: '',
    saldoInicial: 0,
    imagemUrl: ''
  };

  arquivoSelecionado: File | null = null;
  previewUrlLocal = signal<string | null>(null);
  isUploading = signal<boolean>(false);
  isAnalyzingWithAi = signal<boolean>(false);
  sugestaoIa = signal<string | null>(null);

  onFileSelected(event: Event): void {
    const inputEl = event.target as HTMLInputElement;
    if (inputEl.files && inputEl.files.length > 0) {
      const file = inputEl.files[0];
      this.arquivoSelecionado = file;
      this.previewUrlLocal.set(URL.createObjectURL(file));
      this.novoProduto.imagemUrl = '';

      // Consulta Rápida por IA: Pergunta ao backend o que tem na foto
      this.analisarFotoComIa(file);
    }
  }

  analisarFotoComIa(file: File): void {
    this.isAnalyzingWithAi.set(true);
    this.sugestaoIa.set(null);

    this.produtoService.reconhecerImagem(file).subscribe({
      next: (res) => {
        this.isAnalyzingWithAi.set(false);
        if (res && res.sugestao) {
          this.sugestaoIa.set(res.sugestao);
          // Injeta diretamente no input de Descrição!
          this.novoProduto.descricao = res.sugestao;
          this.toastService.show(`✨ IA identificou: "${res.sugestao}"`, 'success');
        }
      },
      error: () => {
        this.isAnalyzingWithAi.set(false);
      }
    });
  }

  removerArquivo(): void {
    this.arquivoSelecionado = null;
    this.previewUrlLocal.set(null);
    this.sugestaoIa.set(null);
  }

  getImagemExibicao(): string {
    if (this.previewUrlLocal()) {
      return this.previewUrlLocal()!;
    }
    if (this.novoProduto.imagemUrl && this.novoProduto.imagemUrl.trim().length > 0) {
      return this.novoProduto.imagemUrl;
    }
    return '/assets/img/placeholder.svg';
  }

  onClose(): void {
    this.arquivoSelecionado = null;
    this.previewUrlLocal.set(null);
    this.sugestaoIa.set(null);
    this.close.emit();
  }

  onSave(): void {
    if (!this.novoProduto.codigo || !this.novoProduto.descricao) {
      this.toastService.show('Preencha o código e a descrição do produto.', 'error');
      return;
    }

    // Se houver arquivo selecionado, faz o upload físico primeiro no servidor
    if (this.arquivoSelecionado) {
      this.isUploading.set(true);
      this.produtoService.uploadImagem(this.arquivoSelecionado).subscribe({
        next: (res) => {
          this.isUploading.set(false);
          this.novoProduto.imagemUrl = res.url;
          this.save.emit({ ...this.novoProduto });
          this.arquivoSelecionado = null;
          this.previewUrlLocal.set(null);
          this.sugestaoIa.set(null);
        },
        error: (err) => {
          this.isUploading.set(false);
          this.toastService.show(err.error?.mensagem || 'Erro ao enviar imagem para o servidor.', 'error');
        }
      });
    } else {
      this.save.emit({ ...this.novoProduto });
    }
  }
}
