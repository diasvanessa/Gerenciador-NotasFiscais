export enum StatusNotaFiscal {
  Aberta = 0,
  Fechada = 1
}

export interface ItemNotaFiscal {
  id?: string;
  codigoProduto: string;
  quantidade: number;
}

export interface NotaFiscal {
  id?: string;
  numero: number;
  status: StatusNotaFiscal;
  itens: ItemNotaFiscal[];
}

export interface ItemNotaFiscalRequest {
  codigoProduto: string;
  quantidade: number;
}

export interface CriarNotaFiscalRequest {
  itens: ItemNotaFiscalRequest[];
}