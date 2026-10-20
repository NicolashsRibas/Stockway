import { hojeNoFormatoDaApi } from './formatar'

export const LIMITES = {
  codigo: 30,
  nomeMinimo: 2,
  nomeMaximo: 100,
  endereco: 10,
  pesoMaximo: 50000
}

export function formularioVazio() {
  return {
    codigo: '',
    nome: '',
    corredor: '',
    prateleira: '',
    quantidade: '',
    estoqueMinimo: '',
    estoqueMaximo: '',
    valorUnitario: '',
    pesoKg: '',
    validade: ''
  }
}

export function formularioDoProduto(produto) {
  return {
    codigo: produto.codigo,
    nome: produto.nome,
    corredor: produto.endereco.corredor,
    prateleira: produto.endereco.prateleira,
    quantidade: String(produto.quantidade),
    estoqueMinimo: String(produto.estoque_minimo),
    estoqueMaximo: String(produto.estoque_maximo),
    valorUnitario: produto.valor_unitario.toFixed(2).replace('.', ','),
    pesoKg: String(produto.peso_kg).replace('.', ','),
    validade: produto.validade ?? ''
  }
}

export function numeroInteiro(texto) {
  const limpo = String(texto ?? '').trim()
  if (!/^\d+$/.test(limpo)) return null
  const numero = Number(limpo)
  return Number.isSafeInteger(numero) ? numero : null
}

export function numeroDecimal(texto) {
  let limpo = String(texto ?? '').trim()
  if (limpo.includes(',')) limpo = limpo.replace(/\./g, '').replace(',', '.')
  if (!/^\d+(\.\d+)?$/.test(limpo)) return null
  return Number(limpo)
}

function validarEndereco(valor, nomeDoCampo) {
  const texto = valor.trim()
  if (!texto) return `Informe o ${nomeDoCampo}.`
  if (texto.length > LIMITES.endereco) return `Use no máximo ${LIMITES.endereco} caracteres.`
  if (!/^[A-Za-z0-9-]+$/.test(texto)) return 'Use apenas letras, números e hífen.'
  return ''
}

export function validarProduto(form, { cadastro = true } = {}) {
  const erros = {}

  if (cadastro) {
    const codigo = form.codigo.trim()
    if (!codigo) erros.codigo = 'Informe o código do produto.'
    else if (codigo.length > LIMITES.codigo) erros.codigo = `Use no máximo ${LIMITES.codigo} caracteres.`
    else if (!/^[A-Za-z0-9._-]+$/.test(codigo)) erros.codigo = 'Use apenas letras, números, hífen, ponto ou sublinhado.'
  }

  const nome = form.nome.trim()
  if (!nome) erros.nome = 'Informe o nome do produto.'
  else if (nome.length < LIMITES.nomeMinimo || nome.length > LIMITES.nomeMaximo) {
    erros.nome = `O nome precisa ter entre ${LIMITES.nomeMinimo} e ${LIMITES.nomeMaximo} caracteres.`
  }

  const erroDoCorredor = validarEndereco(form.corredor, 'corredor')
  if (erroDoCorredor) erros.corredor = erroDoCorredor
  const erroDaPrateleira = validarEndereco(form.prateleira, 'prateleira')
  if (erroDaPrateleira) erros.prateleira = erroDaPrateleira

  const minimo = numeroInteiro(form.estoqueMinimo)
  const maximo = numeroInteiro(form.estoqueMaximo)
  if (minimo === null) erros.estoqueMinimo = 'Use um número inteiro igual ou maior que zero.'
  if (maximo === null || maximo === 0) erros.estoqueMaximo = 'Use um número inteiro maior que zero.'
  if (minimo !== null && maximo !== null && maximo > 0 && minimo >= maximo) {
    erros.estoqueMinimo = 'O mínimo precisa ser menor que o máximo.'
  }

  if (cadastro && String(form.quantidade).trim()) {
    const quantidade = numeroInteiro(form.quantidade)
    if (quantidade === null) erros.quantidade = 'Use um número inteiro igual ou maior que zero.'
    else if (maximo && quantidade > maximo) erros.quantidade = `Não pode passar do estoque máximo (${maximo}).`
  }

  const valor = numeroDecimal(form.valorUnitario)
  if (!String(form.valorUnitario).trim()) erros.valorUnitario = 'Informe o valor unitário.'
  else if (valor === null) erros.valorUnitario = 'Use um valor como 12,50.'

  const peso = numeroDecimal(form.pesoKg)
  if (!String(form.pesoKg).trim()) erros.pesoKg = 'Informe o peso de uma unidade.'
  else if (peso === null || peso <= 0) erros.pesoKg = 'Use um peso maior que zero, como 0,25.'
  else if (peso > LIMITES.pesoMaximo) erros.pesoKg = 'O peso passa do limite de 50.000 kg.'

  if (form.validade && form.validade < hojeNoFormatoDaApi()) erros.validade = 'Essa data já passou.'

  return erros
}

export function montarProdutoNovo(form) {
  const produto = {
    codigo: form.codigo.trim().toUpperCase(),
    nome: form.nome.trim(),
    quantidade: numeroInteiro(form.quantidade) ?? 0,
    valor_unitario: numeroDecimal(form.valorUnitario),
    peso_kg: numeroDecimal(form.pesoKg),
    endereco: { corredor: form.corredor.trim().toUpperCase(), prateleira: form.prateleira.trim().toUpperCase() },
    estoque_minimo: numeroInteiro(form.estoqueMinimo),
    estoque_maximo: numeroInteiro(form.estoqueMaximo)
  }
  if (form.validade) produto.validade = form.validade
  return produto
}

export function montarAlteracoes(form, produto) {
  const novo = montarProdutoNovo({ ...form, codigo: produto.codigo })
  const alteracoes = {}
  if (novo.nome !== produto.nome) alteracoes.nome = novo.nome
  if (novo.valor_unitario !== produto.valor_unitario) alteracoes.valor_unitario = novo.valor_unitario
  if (novo.peso_kg !== produto.peso_kg) alteracoes.peso_kg = novo.peso_kg
  if (novo.estoque_minimo !== produto.estoque_minimo) alteracoes.estoque_minimo = novo.estoque_minimo
  if (novo.estoque_maximo !== produto.estoque_maximo) alteracoes.estoque_maximo = novo.estoque_maximo
  if (novo.endereco.corredor !== produto.endereco.corredor || novo.endereco.prateleira !== produto.endereco.prateleira) {
    alteracoes.endereco = novo.endereco
  }
  if (!form.validade && produto.validade) alteracoes.remover_validade = true
  else if (form.validade && form.validade !== produto.validade) alteracoes.validade = form.validade
  return alteracoes
}
