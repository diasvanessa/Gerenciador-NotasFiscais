import { Component, input, output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Produto } from '../../../models/produto.model';
import { CriarNotaFiscalRequest, ItemNotaFiscalRequest } from '../../../models/nota-fiscal.model';

@Component({
  selector: 'app-modal-nota',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './modal-nota.component.html',
  styleUrl: './modal-nota.component.css'
})
export class ModalNotaComponent {
  readonly isOpen = input<boolean>(false);
  readonly produtos = input<Produto[]>([]);

  readonly close = output<void>();
  readonly save = output<CriarNotaFiscalRequest>();

  itensNovaNota: ItemNotaFiscalRequest[] = [
    { codigoProduto: '', quantidade: 1 }
  ];

  onClose(): void {
    this.close.emit();
  }

  adicionarItem(): void {
    const primeiroCodigo = this.produtos()[0]?.codigo || '';
    this.itensNovaNota.push({ codigoProduto: primeiroCodigo, quantidade: 1 });
  }

  removerItem(index: number): void {
    if (this.itensNovaNota.length > 1) {
      this.itensNovaNota.splice(index, 1);
    }
  }

  onSave(): void {
    this.save.emit({ itens: [...this.itensNovaNota] });
  }
}
