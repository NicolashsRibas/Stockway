import { ErroDaApi } from './ErroDaApi'

export const ENDERECO_DA_API = (import.meta.env.VITE_API_URL || 'http://localhost:8080/api').replace(/\/$/, '')

function montarConsulta(parametros = {}) {
  const consulta = new URLSearchParams()
  Object.entries(parametros).forEach(([chave, valor]) => {
    if (valor !== undefined && valor !== null && valor !== '') consulta.append(chave, valor)
  })
  const texto = consulta.toString()
  return texto ? `?${texto}` : ''
}

export async function requisicao(metodo, caminho, { corpo, parametros } = {}) {
  const opcoes = { method: metodo, headers: {} }
  if (corpo !== undefined) {
    opcoes.headers['Content-Type'] = 'application/json'
    opcoes.body = JSON.stringify(corpo)
  }

  let resposta
  try {
    resposta = await fetch(`${ENDERECO_DA_API}${caminho}${montarConsulta(parametros)}`, opcoes)
  } catch {
    throw new ErroDaApi('Não foi possível falar com a API. Confira se o back-end está rodando.')
  }

  let json = null
  try {
    json = await resposta.json()
  } catch {
    throw new ErroDaApi('A API respondeu em um formato inesperado.', resposta.status)
  }

  if (!json.sucesso) throw new ErroDaApi(primeiraLetraMaiuscula(json.erro || 'erro desconhecido'), resposta.status)
  return json.dados
}

function primeiraLetraMaiuscula(texto) {
  return texto.charAt(0).toUpperCase() + texto.slice(1)
}
