import { ref } from 'vue'
import { produtosComEstoqueBaixo } from '../services/estoqueService'

export const quantidadeParaRepor = ref(0)

export async function atualizarAlertas() {
  try {
    const resposta = await produtosComEstoqueBaixo()
    quantidadeParaRepor.value = resposta.total
  } catch {
    quantidadeParaRepor.value = 0
  }
}
