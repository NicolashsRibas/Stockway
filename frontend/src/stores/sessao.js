import { ref } from 'vue'

const CHAVE = 'stockway:usuario'

function lerUsuarioSalvo() {
  try {
    const salvo = localStorage.getItem(CHAVE) || sessionStorage.getItem(CHAVE)
    return salvo ? JSON.parse(salvo) : null
  } catch {
    return null
  }
}

export const usuario = ref(lerUsuarioSalvo())

export function entrar(dados, lembrar) {
  usuario.value = dados
  try {
    const lugar = lembrar ? localStorage : sessionStorage
    lugar.setItem(CHAVE, JSON.stringify(dados))
  } catch {
    return
  }
}

export function sair() {
  usuario.value = null
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
