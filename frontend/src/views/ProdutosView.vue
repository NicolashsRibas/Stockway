<script setup>
import { ref, computed, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import IconeSvg from '../components/IconeSvg.vue'
import SeloCorredor from '../components/SeloCorredor.vue'
import SeloStatus from '../components/SeloStatus.vue'
import SeloValidade from '../components/SeloValidade.vue'
import NivelEstoque from '../components/NivelEstoque.vue'
import CelulaProduto from '../components/CelulaProduto.vue'
import PaginacaoTabela from '../components/PaginacaoTabela.vue'
import AlertaAviso from '../components/AlertaAviso.vue'
import { listarProdutos } from '../services/produtosService'
import { STATUS_DA_VALIDADE } from '../utils/estoque'
import { formatarNumero, formatarDinheiro, formatarData, semAcento } from '../utils/formatar'

const ITENS_POR_PAGINA = 8
const FILTROS_DE_STATUS = [
  { valor: 'todos', rotulo: 'Todos' },
  { valor: 'repor', rotulo: 'Para repor' },
  { valor: 'NORMAL', rotulo: 'Em estoque' },
  { valor: 'ACIMA_MAXIMO', rotulo: 'Acima do máximo' }
]
const ORDENACOES = [
  { valor: 'nome', rotulo: 'Ordenar por nome' },
  { valor: 'codigo', rotulo: 'Ordenar por código' },
  { valor: 'urgencia', rotulo: 'Mais urgentes primeiro' },
  { valor: 'quantidade', rotulo: 'Menor quantidade' },
  { valor: 'valor_em_estoque', rotulo: 'Maior valor em estoque' },
  { valor: 'endereco', rotulo: 'Endereço' },
  { valor: 'validade', rotulo: 'Validade mais próxima' }
]

const rota = useRoute()
const roteador = useRouter()

const produtos = ref([])
const carregando = ref(true)
const erro = ref('')
const pagina = ref(1)

const busca = computed(() => rota.query.busca ?? '')
const status = computed(() => rota.query.status ?? 'todos')
const corredor = computed(() => rota.query.corredor ?? '')
const validade = computed(() => rota.query.validade ?? '')
const ordenar = computed(() => rota.query.ordenar ?? 'nome')

async function carregar() {
  carregando.value = true
  erro.value = ''
  try {
    const resposta = await listarProdutos({ ordenar: ordenar.value })
    produtos.value = resposta.itens
  } catch (falha) {
    erro.value = falha.message
  } finally {
    carregando.value = false
  }
}

watch(ordenar, carregar, { immediate: true })

watch([busca, status, corredor, validade], () => {
  pagina.value = 1
})

function passaNoStatus(produto, filtro) {
  if (filtro === 'todos') return true
  if (filtro === 'repor') return produto.baixo_estoque
  return produto.status === filtro
}

const produtosFiltrados = computed(() => {
  const termo = semAcento(busca.value.trim())
  return produtos.value.filter((produto) => {
    if (termo && !semAcento(`${produto.codigo} ${produto.nome}`).includes(termo)) return false
    if (corredor.value && produto.endereco.corredor !== corredor.value) return false
    if (validade.value === 'VENCE_EM_BREVE' && !['VENCE_EM_BREVE', 'VENCIDO'].includes(produto.status_validade)) return false
    if (validade.value && validade.value !== 'VENCE_EM_BREVE' && produto.status_validade !== validade.value) return false
    return passaNoStatus(produto, status.value)
  })
})

const totalDePaginas = computed(() => Math.max(1, Math.ceil(produtosFiltrados.value.length / ITENS_POR_PAGINA)))
const produtosDaPagina = computed(() => {
  const inicio = (pagina.value - 1) * ITENS_POR_PAGINA
  return produtosFiltrados.value.slice(inicio, inicio + ITENS_POR_PAGINA)
})

const corredoresExistentes = computed(() =>
  [...new Set(produtos.value.map((produto) => produto.endereco.corredor))].sort()
)

const contagem = computed(() =>
  Object.fromEntries(
    FILTROS_DE_STATUS.map((filtro) => [filtro.valor, produtos.value.filter((produto) => passaNoStatus(produto, filtro.valor)).length])
  )
)

const valorTotal = computed(() => produtosFiltrados.value.reduce((total, produto) => total + produto.valor_em_estoque, 0))

function mudarFiltro(nome, valor) {
  const query = { ...rota.query }
  if (!valor || valor === 'todos' || (nome === 'ordenar' && valor === 'nome')) delete query[nome]
  else query[nome] = valor
  roteador.replace({ query })
}
</script>

<template>
  <div class="pagina">
    <div class="cabecalho-pagina">
      <div>
        <h2 class="t-h2">Produtos em estoque</h2>
        <p class="t-small">Pesquise, filtre e acompanhe o nível de cada item entre o mínimo e o máximo.</p>
      </div>
      <div class="mini-stats">
        <span class="mini-stat"><i style="background: var(--brand-600)" /> <strong>{{ formatarNumero(contagem.todos) }}</strong> produtos</span>
        <span class="mini-stat"><i style="background: var(--ok)" /> <strong>{{ formatarDinheiro(valorTotal) }}</strong> em estoque</span>
        <span class="mini-stat"><i style="background: var(--low)" /> <strong>{{ contagem.repor }}</strong> para repor</span>
      </div>
    </div>

    <AlertaAviso v-if="erro" tom="erro">{{ erro }}</AlertaAviso>

    <section class="cartao">
      <div class="filtros">
        <button
          v-for="filtro in FILTROS_DE_STATUS"
          :key="filtro.valor"
          type="button"
          class="filtros__chip"
          :aria-pressed="status === filtro.valor"
          @click="mudarFiltro('status', filtro.valor)"
        >
          <IconeSvg v-if="filtro.valor === 'repor'" nome="alerta" :tamanho="15" />
          {{ filtro.rotulo }}
          <span class="filtros__chip-n">{{ contagem[filtro.valor] }}</span>
        </button>

        <select :value="corredor" aria-label="Filtrar por corredor" @change="mudarFiltro('corredor', $event.target.value)">
          <option value="">Todos os corredores</option>
          <option v-for="item in corredoresExistentes" :key="item" :value="item">Corredor {{ item }}</option>
        </select>

        <select :value="validade" aria-label="Filtrar por validade" @change="mudarFiltro('validade', $event.target.value)">
          <option value="">Qualquer validade</option>
          <option value="VENCE_EM_BREVE">Vencidos ou vencendo</option>
          <option v-for="(informacao, chave) in STATUS_DA_VALIDADE" :key="chave" :value="chave">{{ informacao.rotulo }}</option>
        </select>

        <select :value="ordenar" aria-label="Ordenar" @change="mudarFiltro('ordenar', $event.target.value)">
          <option v-for="opcao in ORDENACOES" :key="opcao.valor" :value="opcao.valor">{{ opcao.rotulo }}</option>
        </select>

        <span v-if="busca" class="t-small fraco">
          Pesquisa: <strong>“{{ busca }}”</strong>
          <button type="button" class="link link--botao" @click="mudarFiltro('busca', '')">limpar</button>
        </span>
      </div>

      <div class="tabela-rolagem">
        <table class="tabela">
          <thead>
            <tr>
              <th>Produto</th>
              <th>Endereço</th>
              <th class="num">Quantidade</th>
              <th class="num">Mín. / Máx.</th>
              <th class="num">Valor em estoque</th>
              <th>Validade</th>
              <th>Status</th>
              <th><span class="sr-only">Ações</span></th>
            </tr>
          </thead>
          <tbody>
            <tr v-if="carregando">
              <td colspan="8" class="tabela__vazia">Carregando produtos…</td>
            </tr>
            <tr v-for="produto in produtosDaPagina" :key="produto.id" :class="{ 'linha--baixo': produto.baixo_estoque }">
              <td>
                <CelulaProduto :nome="produto.nome" :codigo="produto.codigo" :corredor="produto.endereco.corredor" :tamanho-do-icone="36" />
              </td>
              <td>
                <span class="celula-endereco">
                  <SeloCorredor :corredor="produto.endereco.corredor" />
                  <span class="t-label fraco">Prateleira {{ produto.endereco.prateleira }}</span>
                </span>
              </td>
              <td class="num num--qtd">
                {{ formatarNumero(produto.quantidade) }}
                <NivelEstoque :quantidade="produto.quantidade" :maximo="produto.estoque_maximo" :status="produto.status" />
              </td>
              <td class="num fraco">{{ formatarNumero(produto.estoque_minimo) }} / {{ formatarNumero(produto.estoque_maximo) }}</td>
              <td class="num">{{ formatarDinheiro(produto.valor_em_estoque) }}</td>
              <td>
                <span class="celula-validade">
                  <span class="t-small">{{ formatarData(produto.validade) }}</span>
                  <SeloValidade
                    v-if="produto.status_validade !== 'NAO_PERECIVEL' && produto.status_validade !== 'OK'"
                    :status="produto.status_validade"
                    :dias="produto.dias_para_vencer"
                  />
                </span>
              </td>
              <td><SeloStatus :status="produto.status" /></td>
              <td class="num">
                <div class="acoes-linha">
                  <RouterLink
                    class="btn-icone btn-icone--entrada"
                    :to="{ path: '/entradas', query: { produto: produto.codigo } }"
                    title="Registrar entrada"
                    :aria-label="`Registrar entrada de ${produto.nome}`"
                  >
                    <IconeSvg nome="entrada" :tamanho="16" />
                  </RouterLink>
                  <RouterLink
                    class="btn-icone"
                    :to="{ path: '/saidas', query: { produto: produto.codigo } }"
                    title="Registrar saída"
                    :aria-label="`Registrar saída de ${produto.nome}`"
                  >
                    <IconeSvg nome="saida" :tamanho="16" />
                  </RouterLink>
                  <RouterLink
                    class="btn-icone"
                    :to="`/produtos/${encodeURIComponent(produto.codigo)}`"
                    title="Ver e editar"
                    :aria-label="`Ver e editar ${produto.nome}`"
                  >
                    <IconeSvg nome="lapis" :tamanho="16" />
                  </RouterLink>
                </div>
              </td>
            </tr>
            <tr v-if="!carregando && produtosDaPagina.length === 0">
              <td colspan="8" class="tabela__vazia">Nenhum produto encontrado com esses filtros.</td>
            </tr>
          </tbody>
        </table>
      </div>

      <footer class="tabela__rodape t-small">
        <span>
          Mostrando {{ produtosDaPagina.length }} de {{ produtosFiltrados.length }}
          produto{{ produtosFiltrados.length === 1 ? '' : 's' }}
        </span>
        <PaginacaoTabela v-model="pagina" :total-de-paginas="totalDePaginas" />
      </footer>
    </section>
  </div>
</template>
