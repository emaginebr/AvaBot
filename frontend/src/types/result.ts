export interface Result<T> {
  sucesso: boolean;
  mensagem: string;
  erros: string[];
  dados: T;
}

export interface PaginatedResult<T> {
  items: T[];
  total: number;
  page: number;
  maxPage: number;
}
