import { ref, computed } from 'vue'

// Sessão de quem entrou: o token da API e os dados do usuário (nome, cargo, e-mail).
// Com "manter conectado", fica no localStorage; sem, no sessionStorage (some ao fechar o navegador).
const CHAVE = 'stockway:sessao'
const CHAVE_ANTIGA = 'stockway:usuario' // da tela antiga, que só pedia o nome

export const MENSAGEM_SESSAO_EXPIRADA = 'Sua sessão expirou. Entre de novo para continuar.'

function sessaoValida(sessao) {
  return Boolean(sessao?.token && sessao?.usuario?.nome && Date.parse(sessao.expiraEm) > Date.now())
}

function lerSessaoSalva() {
  try {
    localStorage.removeItem(CHAVE_ANTIGA)
    sessionStorage.removeItem(CHAVE_ANTIGA)
    const salvo = localStorage.getItem(CHAVE) || sessionStorage.getItem(CHAVE)
    const sessao = salvo ? JSON.parse(salvo) : null
    return sessaoValida(sessao) ? sessao : null
  } catch {
    return null
  }
}

const sessao = ref(lerSessaoSalva())

export const usuario = computed(() => sessao.value?.usuario ?? null)

// Mensagem mostrada na tela de entrada quando a pessoa foi desconectada sem querer.
export const avisoDeSaida = ref('')

export function tokenDeAcesso() {
  return sessaoValida(sessao.value) ? sessao.value.token : null
}

export function estaLogado() {
  return tokenDeAcesso() !== null
}

/** Guarda a resposta de POST /auth/login: { token, expira_em, usuario }. */
export function entrar(resposta, lembrar) {
  const nova = { token: resposta.token, expiraEm: resposta.expira_em, usuario: resposta.usuario }
  sessao.value = nova
  avisoDeSaida.value = ''
  try {
    localStorage.removeItem(CHAVE)
    sessionStorage.removeItem(CHAVE)
    const lugar = lembrar ? localStorage : sessionStorage
    lugar.setItem(CHAVE, JSON.stringify(nova))
  } catch {
    return
  }
}

export function sair(aviso = '') {
  sessao.value = null
  avisoDeSaida.value = aviso
  try {
    localStorage.removeItem(CHAVE)
    sessionStorage.removeItem(CHAVE)
  } catch {
    return
  }
}

export function nomeDoResponsavel() {
  return usuario.value?.nome ?? ''
}
