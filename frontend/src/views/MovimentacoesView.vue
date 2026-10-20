<script setup>
import { ref, computed, watch, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import IconeSvg from '../components/IconeSvg.vue'
import CelulaTipo from '../components/CelulaTipo.vue'
import CelulaProduto from '../components/CelulaProduto.vue'
import CelulaPessoa from '../components/CelulaPessoa.vue'
import PaginacaoTabela from '../components/PaginacaoTabela.vue'
import AlertaAviso from '../components/AlertaAviso.vue'
import { listarProdutos } from '../services/produtosService'
import { listarMovimentacoes } from '../services/movimentacoesService'
import { quantidadeComSinal, TIPOS_DE_MOVIMENTACAO } from '../utils/estoque'
import { formatarNumero, formatarDinheiro, formatarDataHora, hojeNoFormatoDaApi } from '../utils/formatar'

const ITENS_POR_PAGINA = 12
const FILTROS_DE_TIPO = [
  { valor: '', rotulo: 'Todas' },
  { valor: 'ENTRADA', rotulo: 'Entradas', icone: 'entrada' },
  { valor: 'SAIDA', rotulo: 'Saídas', icone: 'saida' },
  { valor: 'AJUSTE', rotulo: 'Ajustes', icone: 'ajuste' },
  { valor: 'CADASTRO', rotulo: 'Cadastros', icone: 'mais' },
  { valor: 'EDICAO', rotulo: 'Edições', icone: 'lapis' }
]
const PERIODOS = [
  { valor: '7', rotulo: 'Últimos 7 dias' },
  { valor: '30', rotulo: 'Últimos 30 dias' },
  { valor: '', rotulo: 'Tudo' }
]

const rota = useRoute()
const roteador = useRouter()

const produtos = ref([])
const movimentacoes = ref([])
const total = ref(0)
const carregando = ref(true)
const erro = ref('')
const periodo = ref('30')
const codigo = ref('')
const pagina = ref(1)

const tipo = computed(() => rota.query.tipo ?? '')
const totalDePaginas = computed(() => Math.max(1, Math.ceil(total.value / ITENS_POR_PAGINA)))

async function carregar() {
  carregando.value = true
  erro.value = ''
  try {
    const resposta = await listarMovimentacoes({
      tipo: tipo.value,
      codigo: codigo.value,
      data_inicio: periodo.value ? hojeNoFormatoDaApi(-(Number(periodo.value) - 1)) : '',
      limite: ITENS_POR_PAGINA,
      offset: (pagina.value - 1) * ITENS_POR_PAGINA
    })
    movimentacoes.value = resposta.itens
    total.value = resposta.total
  } catch (falha) {
    erro.value = falha.message
  } finally {
    carregando.value = false
  }
}

onMounted(async () => {
  try {
    const resposta = await listarProdutos({ incluir_inativos: true })
    produtos.value = resposta.itens
  } catch {
    produtos.value = []
  }
})

watch([tipo, periodo, codigo], () => {
  if (pagina.value === 1) carregar()
  else pagina.value = 1
})

watch(pagina, carregar, { immediate: true })

const somaDaPagina = computed(() => ({
  entradas: movimentacoes.value.filter((item) => item.tipo === 'ENTRADA').reduce((soma, item) => soma + item.quantidade, 0),
  saidas: movimentacoes.value.filter((item) => item.tipo === 'SAIDA').reduce((soma, item) => soma + item.quantidade, 0)
}))

function trocarTipo(novoTipo) {
  const query = { ...rota.query }
  if (novoTipo) query.tipo = novoTipo
  else delete query.tipo
  roteador.replace({ query })
}
</script>

<template>
  <div class="pagina">
    <div class="cabecalho-pagina">
      <div>
        <h2 class="t-h2">Histórico de movimentações</h2>
        <p class="t-small">Tudo o que aconteceu no estoque, com o saldo antes e depois de cada registro.</p>
      </div>
      <div class="mini-stats">
        <span class="mini-stat"><i style="background: var(--brand-600)" /> <strong>{{ formatarNumero(total) }}</strong> registros</span>
        <span class="mini-stat"><i style="background: var(--ok)" /> <strong>+{{ formatarNumero(somaDaPagina.entradas) }}</strong> entraram nesta página</span>
        <span class="mini-stat"><i style="background: var(--low)" /> <strong>−{{ formatarNumero(somaDaPagina.saidas) }}</strong> saíram nesta página</span>
      </div>
    </div>

    <AlertaAviso v-if="erro" tom="erro">{{ erro }}</AlertaAviso>

    <section class="cartao">
      <div class="filtros">
        <button
          v-for="filtro in FILTROS_DE_TIPO"
          :key="filtro.valor"
          type="button"
          class="filtros__chip"
          :aria-pressed="tipo === filtro.valor"
          @click="trocarTipo(filtro.valor)"
        >
          <IconeSvg v-if="filtro.icone" :nome="filtro.icone" :tamanho="15" />
          {{ filtro.rotulo }}
        </button>

        <select v-model="periodo" aria-label="Período">
          <option v-for="opcao in PERIODOS" :key="opcao.valor" :value="opcao.valor">{{ opcao.rotulo }}</option>
        </select>

        <select v-model="codigo" class="filtros__produto" aria-label="Filtrar por produto">
          <option value="">Todos os produtos</option>
          <option v-for="produto in produtos" :key="produto.codigo" :value="produto.codigo">
            {{ produto.codigo }} — {{ produto.nome }}
          </option>
        </select>
      </div>

      <div class="tabela-rolagem">
        <table class="tabela">
          <thead>
            <tr>
              <th>Data</th>
              <th>Tipo</th>
              <th>Produto</th>
              <th class="num">Quantidade</th>
              <th class="num">Saldo</th>
              <th class="num">Valor</th>
              <th>Responsável</th>
              <th>Observação</th>
            </tr>
          </thead>
          <tbody>
            <tr v-if="carregando">
              <td colspan="8" class="tabela__vazia">Carregando movimentações…</td>
            </tr>
            <tr v-for="movimentacao in movimentacoes" :key="movimentacao.id">
              <td class="fraco sem-quebra">{{ formatarDataHora(movimentacao.data_hora) }}</td>
              <td><CelulaTipo :tipo="movimentacao.tipo" /></td>
              <td><CelulaProduto :nome="movimentacao.produto_nome" :codigo="movimentacao.produto_codigo" /></td>
              <td :class="['num', `qtd--${TIPOS_DE_MOVIMENTACAO[movimentacao.tipo]?.classe}`]">{{ quantidadeComSinal(movimentacao) }}</td>
              <td class="num sem-quebra">
                {{ formatarNumero(movimentacao.saldo_anterior) }} → <strong>{{ formatarNumero(movimentacao.saldo_atual) }}</strong>
              </td>
              <td class="num">{{ movimentacao.valor_total ? formatarDinheiro(Math.abs(movimentacao.valor_total)) : '—' }}</td>
              <td><CelulaPessoa :nome="movimentacao.responsavel" /></td>
              <td class="fraco texto-quebra">{{ movimentacao.observacao || '—' }}</td>
            </tr>
            <tr v-if="!carregando && movimentacoes.length === 0">
              <td colspan="8" class="tabela__vazia">Nenhuma movimentação com esses filtros.</td>
            </tr>
          </tbody>
        </table>
      </div>

      <footer class="tabela__rodape t-small">
        <span>{{ formatarNumero(total) }} registro{{ total === 1 ? '' : 's' }} encontrados</span>
        <PaginacaoTabela v-model="pagina" :total-de-paginas="totalDePaginas" :mostrar-numeros="false" />
      </footer>
    </section>
  </div>
</template>
