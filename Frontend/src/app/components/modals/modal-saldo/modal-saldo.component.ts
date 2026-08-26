import { Component, input, output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Produto } from '../../../models/produto.model';

@Component({
  selector: 'app-modal-saldo',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './modal-saldo.component.html',
  styleUrl: './modal-saldo.component.css'
})
export class ModalSaldoComponent {
  readonly isOpen = input<boolean>(false);
  readonly produto = input<Produto | null>(null);
  readonly tipo = input<'baixar' | 'estornar'>('baixar');

  readonly close = output<void>();
  readonly save = output<{ quantidade: number }>();

  quantidade: number = 1;

  getImagemProduto(url?: string | null): string {
    if (url && url.trim().length > 0) {
      return url;
    }
    return '/assets/img/placeholder.svg';
  }

  onClose(): void {
    this.close.emit();
  }

  onSave(): void {
    this.save.emit({ quantidade: this.quantidade });
  }
}
