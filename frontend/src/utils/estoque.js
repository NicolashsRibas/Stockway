export const STATUS_DO_ESTOQUE = {
  ZERADO: { rotulo: 'Zerado', classe: 'selo--baixo' },
  BAIXO: { rotulo: 'Estoque baixo', classe: 'selo--baixo' },
  NORMAL: { rotulo: 'Em estoque', classe: 'selo--ok' },
  ACIMA_MAXIMO: { rotulo: 'Acima do máximo', classe: 'selo--atencao' }
}

export const STATUS_DA_VALIDADE = {
  NAO_PERECIVEL: { rotulo: 'Não perecível', classe: 'selo--neutro' },
  OK: { rotulo: 'No prazo', classe: 'selo--ok' },
  VENCE_EM_BREVE: { rotulo: 'Vence em breve', classe: 'selo--atencao' },
  VENCIDO: { rotulo: 'Vencido', classe: 'selo--baixo' }
}

export const TIPOS_DE_MOVIMENTACAO = {
  ENTRADA: { rotulo: 'Entrada', icone: 'entrada', classe: 'entrada', sinal: '+' },
  SAIDA: { rotulo: 'Saída', icone: 'saida', classe: 'saida', sinal: '−' },
  AJUSTE: { rotulo: 'Ajuste', icone: 'ajuste', classe: 'ajuste', sinal: '' },
  CADASTRO: { rotulo: 'Cadastro', icone: 'mais', classe: 'cadastro', sinal: '+' },
  EDICAO: { rotulo: 'Edição', icone: 'lapis', classe: 'edicao', sinal: '' },
  REMOCAO: { rotulo: 'Remoção', icone: 'lixeira', classe: 'remocao', sinal: '' }
}

export function quantidadeComSinal(movimentacao) {
  if (movimentacao.tipo === 'EDICAO' || movimentacao.tipo === 'REMOCAO') return '—'
  if (movimentacao.tipo === 'AJUSTE') {
    return movimentacao.quantidade > 0 ? `+${movimentacao.quantidade}` : `−${Math.abs(movimentacao.quantidade)}`
  }
  return `${TIPOS_DE_MOVIMENTACAO[movimentacao.tipo]?.sinal ?? ''}${movimentacao.quantidade}`
}

export function precisaRepor(produto) {
  return produto.baixo_estoque
}
