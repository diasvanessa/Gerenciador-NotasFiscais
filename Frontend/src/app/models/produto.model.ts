export interface Produto {
  id?: string;
  codigo: string;
  descricao: string;
  saldo: number;
  imagemUrl?: string | null;
}

export interface CadastrarProdutoRequest {
  codigo: string;
  descricao: string;
  saldoInicial: number;
  imagemUrl?: string | null;
}
