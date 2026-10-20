<script setup>
import { ref, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import IconeSvg from '../components/IconeSvg.vue'
import BotaoBase from '../components/BotaoBase.vue'
import AlertaAviso from '../components/AlertaAviso.vue'
import IconeCorredor from '../components/IconeCorredor.vue'
import SeloCorredor from '../components/SeloCorredor.vue'
import SeloStatus from '../components/SeloStatus.vue'
import SeloValidade from '../components/SeloValidade.vue'
import CelulaTipo from '../components/CelulaTipo.vue'
import CelulaPessoa from '../components/CelulaPessoa.vue'
import FormularioProduto from '../components/FormularioProduto.vue'
import { buscarProduto, editarProduto, removerProduto, historicoDoProduto } from '../services/produtosService'
import { atualizarAlertas } from '../stores/alertas'
import { quantidadeComSinal, TIPOS_DE_MOVIMENTACAO } from '../utils/estoque'
import { formularioDoProduto, validarProduto, montarAlteracoes } from '../utils/validarProduto'
import { formatarNumero, formatarDinheiro, formatarPeso, formatarDataHora, formatarData } from '../utils/formatar'

const props = defineProps({
  codigo: { type: String, required: true }
})

const roteador = useRouter()

const produto = ref(null)
const historico = ref([])
const formulario = ref(null)
const erros = ref({})
const carregando = ref(true)
const naoEncontrado = ref(false)
const salvando = ref(false)
const mensagem = ref('')
const erroDaApi = ref('')

const paraRepor = computed(() => produto.value?.baixo_estoque)

async function carregarHistorico() {
  const resposta = await historicoDoProduto(props.codigo, { limite: 200 })
  historico.value = resposta.itens
}

onMounted(async () => {
  try {
    const resposta = await buscarProduto(props.codigo)
    produto.value = resposta.produto
    formulario.value = formularioDoProduto(resposta.produto)
    await carregarHistorico()
  } catch (falha) {
    if (falha.status === 404) naoEncontrado.value = true
    else erroDaApi.value = falha.message
  } finally {
    carregando.value = false
  }
})

function aoMudarCampo() {
  mensagem.value = ''
  erroDaApi.value = ''
}

async function salvar() {
  erros.value = validarProduto(formulario.value, { cadastro: false })
  if (Object.keys(erros.value).length > 0) return

  const alteracoes = montarAlteracoes(formulario.value, produto.value)
  if (Object.keys(alteracoes).length === 0) {
    mensagem.value = 'Nada mudou, então não foi preciso salvar.'
    return
  }

  salvando.value = true
  try {
    produto.value = await editarProduto(produto.value.codigo, alteracoes)
    formulario.value = formularioDoProduto(produto.value)
    mensagem.value = 'Alterações salvas.'
    await carregarHistorico()
    atualizarAlertas()
  } catch (falha) {
    erroDaApi.value = falha.message
  } finally {
    salvando.value = false
  }
}

function desfazer() {
  formulario.value = formularioDoProduto(produto.value)
  erros.value = {}
  mensagem.value = ''
  erroDaApi.value = ''
}

async function remover() {
  const confirmou = window.confirm(`Remover ${produto.value.codigo} — ${produto.value.nome} do cadastro? O histórico continua guardado.`)
  if (!confirmou) return
  try {
    await removerProduto(produto.value.codigo, 'Removido pela tela de produtos')
    atualizarAlertas()
    roteador.push('/produtos')
  } catch (falha) {
    erroDaApi.value = falha.message
  }
}
</script>

<template>
  <p v-if="carregando" class="carregando-tela">Carregando produto…</p>

  <AlertaAviso v-else-if="naoEncontrado" tom="erro">
    Produto {{ codigo }} não encontrado.
    <template #acao><RouterLink class="link" to="/produtos">Voltar para a lista</RouterLink></template>
  </AlertaAviso>

  <AlertaAviso v-else-if="!produto" tom="erro">{{ erroDaApi }}</AlertaAviso>

  <div v-else class="pagina pagina--estreita">
    <AlertaAviso v-if="!produto.ativo" tom="erro">Este produto foi removido do cadastro. Só o histórico continua disponível.</AlertaAviso>
    <AlertaAviso v-if="mensagem" tom="ok">{{ mensagem }}</AlertaAviso>
    <AlertaAviso v-if="erroDaApi" tom="erro">{{ erroDaApi }}</AlertaAviso>

    <div class="ficha">
      <IconeCorredor :corredor="produto.endereco.corredor" :tamanho="64" />
      <div class="ficha__texto">
        <span class="t-label ficha__codigo">{{ produto.codigo }}</span>
        <h2 class="t-h2">{{ produto.nome }}</h2>
        <div class="ficha__meta">
          <SeloCorredor :corredor="produto.endereco.corredor" :prateleira="produto.endereco.prateleira" />
          <SeloStatus :status="produto.status" />
          <SeloValidade :status="produto.status_validade" :dias="produto.dias_para_vencer" />
          <span class="t-small"><IconeSvg nome="historico" :tamanho="14" /> {{ historico.length }} registro{{ historico.length === 1 ? '' : 's' }} no histórico</span>
        </div>
      </div>
    </div>

    <div class="resumo">
      <div :class="['resumo__card', paraRepor ? 'resumo__card--baixo' : 'resumo__card--verde']">
        <span class="t-small resumo__rotulo">Saldo atual</span>
        <span class="resumo__valor">{{ formatarNumero(produto.quantidade) }}</span>
        <span class="resumo__apoio">
          <template v-if="paraRepor">repor <strong>{{ formatarNumero(produto.quantidade_para_repor) }}</strong> para chegar ao máximo</template>
          <template v-else><strong>{{ formatarNumero(produto.percentual_ocupacao) }}%</strong> do estoque máximo</template>
        </span>
      </div>
      <div class="resumo__card resumo__card--violeta">
        <span class="t-small resumo__rotulo">Mínimo / máximo</span>
        <span class="resumo__valor">{{ formatarNumero(produto.estoque_minimo) }} / {{ formatarNumero(produto.estoque_maximo) }}</span>
        <span class="resumo__apoio">validade: <strong>{{ formatarData(produto.validade) }}</strong></span>
      </div>
      <div class="resumo__card resumo__card--azul">
        <span class="t-small resumo__rotulo">Valor em estoque</span>
        <span class="resumo__valor resumo__valor--dinheiro">{{ formatarDinheiro(produto.valor_em_estoque) }}</span>
        <span class="resumo__apoio">
          <strong>{{ formatarDinheiro(produto.valor_unitario) }}</strong> cada · {{ formatarPeso(produto.peso_total_kg) }} no total
        </span>
      </div>
      <div class="resumo__card resumo__card--centro">
        <RouterLink class="btn btn--primario btn--compacto" :to="{ path: '/entradas', query: { produto: produto.codigo } }">
          Registrar entrada
        </RouterLink>
        <RouterLink class="btn btn--secundario btn--compacto" :to="{ path: '/saidas', query: { produto: produto.codigo } }">
          Registrar saída
        </RouterLink>
        <RouterLink class="btn btn--secundario btn--compacto" :to="{ path: '/ajustes', query: { produto: produto.codigo } }">
          Ajustar inventário
        </RouterLink>
      </div>
    </div>

    <form
      v-if="produto.ativo"
      class="cadastro__cartao formulario-edicao"
      novalidate
      @submit.prevent="salvar"
      @input="aoMudarCampo"
      @change="aoMudarCampo"
    >
      <header class="cadastro__cabecalho">
        <h2 class="t-h3">Dados do produto</h2>
        <p class="t-small cadastro__subtitulo">A quantidade só muda por entrada, saída ou ajuste. Cada alteração fica no histórico.</p>
      </header>

      <FormularioProduto v-model="formulario" :erros="erros" :cadastro="false" />

      <footer class="cadastro__rodape">
        <div class="cadastro__acoes">
          <RouterLink class="link" to="/produtos">← Voltar para a lista</RouterLink>
          <BotaoBase
            variante="secundario"
            icone="lixeira"
            :desativado="produto.quantidade > 0"
            :title="produto.quantidade > 0 ? 'Só dá para remover com saldo zero' : 'Remover produto'"
            @click="remover"
          >
            Remover
          </BotaoBase>
        </div>
        <div class="cadastro__acoes">
          <BotaoBase variante="secundario" alto @click="desfazer">Desfazer</BotaoBase>
          <BotaoBase tipo="submit" alto icone="certo" :carregando="salvando">Salvar alterações</BotaoBase>
        </div>
      </footer>
    </form>

    <section class="cartao">
      <header class="cartao__cabecalho">
        <h2 class="t-h3">Histórico do produto</h2>
        <span class="t-small cartao__sub">{{ historico.length }} registro{{ historico.length === 1 ? '' : 's' }}</span>
      </header>
      <div class="tabela-rolagem">
        <table class="tabela">
          <thead>
            <tr>
              <th>Data</th>
              <th>Tipo</th>
              <th class="num">Quantidade</th>
              <th class="num">Saldo</th>
              <th>Responsável</th>
              <th>Observação</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="movimentacao in historico" :key="movimentacao.id">
              <td class="fraco sem-quebra">{{ formatarDataHora(movimentacao.data_hora) }}</td>
              <td><CelulaTipo :tipo="movimentacao.tipo" /></td>
              <td :class="['num', `qtd--${TIPOS_DE_MOVIMENTACAO[movimentacao.tipo]?.classe}`]">{{ quantidadeComSinal(movimentacao) }}</td>
              <td class="num sem-quebra">
                {{ formatarNumero(movimentacao.saldo_anterior) }} → <strong>{{ formatarNumero(movimentacao.saldo_atual) }}</strong>
              </td>
              <td><CelulaPessoa :nome="movimentacao.responsavel" /></td>
              <td class="fraco texto-quebra">{{ movimentacao.observacao || '—' }}</td>
            </tr>
            <tr v-if="historico.length === 0">
              <td colspan="6" class="tabela__vazia">Sem movimentações ainda.</td>
            </tr>
          </tbody>
        </table>
      </div>
    </section>
  </div>
</template>
