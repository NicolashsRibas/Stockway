<script setup>
import { ref, computed, watch, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import IconeSvg from '../components/IconeSvg.vue'
import BotaoBase from '../components/BotaoBase.vue'
import CampoFormulario from '../components/CampoFormulario.vue'
import AlertaAviso from '../components/AlertaAviso.vue'
import IconeCorredor from '../components/IconeCorredor.vue'
import AvatarPessoa from '../components/AvatarPessoa.vue'
import CelulaProduto from '../components/CelulaProduto.vue'
import { listarProdutos } from '../services/produtosService'
import { listarMovimentacoes, registrarEntrada, registrarSaida, registrarAjuste } from '../services/movimentacoesService'
import { atualizarAlertas } from '../stores/alertas'
import { usuario } from '../stores/sessao'
import { quantidadeComSinal } from '../utils/estoque'
import { numeroInteiro, numeroDecimal } from '../utils/validarProduto'
import { formatarNumero, formatarDinheiro, formatarDataHora } from '../utils/formatar'

const props = defineProps({
  tipo: { type: String, required: true }
})

const TEXTOS = {
  entrada: {
    titulo: 'Registrar entrada',
    explicacao: 'Material que chegou ao estoque: soma na quantidade do produto.',
    rotuloDaQuantidade: 'Quantidade que chegou',
    rotuloDaObservacao: 'Origem',
    exemploDaObservacao: 'Ex.: NF 98765, devolução do setor…',
    tipoDaApi: 'ENTRADA',
    tituloDaLista: 'Últimas entradas',
    textoDoBotao: 'Registrar entrada'
  },
  saida: {
    titulo: 'Registrar saída',
    explicacao: 'Material que saiu do estoque: desconta da quantidade do produto.',
    rotuloDaQuantidade: 'Quantidade que saiu',
    rotuloDaObservacao: 'Destino / motivo',
    exemploDaObservacao: 'Ex.: Requisição 554 — manutenção',
    tipoDaApi: 'SAIDA',
    tituloDaLista: 'Últimas saídas',
    textoDoBotao: 'Registrar saída'
  },
  ajuste: {
    titulo: 'Ajuste de inventário',
    explicacao: 'Depois de contar o produto na prateleira, o saldo passa a ser o número contado.',
    rotuloDaQuantidade: 'Quantidade contada',
    rotuloDaObservacao: 'Motivo do ajuste',
    exemploDaObservacao: 'Ex.: Inventário mensal — 2 unidades danificadas',
    tipoDaApi: 'AJUSTE',
    tituloDaLista: 'Últimos ajustes',
    textoDoBotao: 'Registrar ajuste'
  }
}

const rota = useRoute()
const texto = computed(() => TEXTOS[props.tipo])

const produtos = ref([])
const ultimas = ref([])
const codigo = ref(rota.query.produto ? String(rota.query.produto) : '')
const quantidade = ref('')
const valorUnitario = ref('')
const observacao = ref('')
const erros = ref({})
const registrando = ref(false)
const resultado = ref(null)
const erroDaApi = ref('')

async function carregar() {
  try {
    const [respostaDeProdutos, respostaDeMovimentacoes] = await Promise.all([
      listarProdutos(),
      listarMovimentacoes({ tipo: texto.value.tipoDaApi, limite: 8 })
    ])
    produtos.value = respostaDeProdutos.itens
    ultimas.value = respostaDeMovimentacoes.itens
  } catch (falha) {
    erroDaApi.value = falha.message
  }
}

onMounted(carregar)

watch(
  () => rota.query.produto,
  (novo) => {
    codigo.value = novo ? String(novo) : ''
  }
)

const produto = computed(() => produtos.value.find((item) => item.codigo === codigo.value) ?? null)
const quantidadeDigitada = computed(() => numeroInteiro(quantidade.value))

const saldoDepois = computed(() => {
  if (!produto.value || quantidadeDigitada.value === null) return null
  if (props.tipo === 'ajuste') return quantidadeDigitada.value
  if (quantidadeDigitada.value === 0) return null
  if (props.tipo === 'entrada') return produto.value.quantidade + quantidadeDigitada.value
  return produto.value.quantidade - quantidadeDigitada.value
})

const corDoSaldoDepois = computed(() => {
  if (saldoDepois.value === null) return ''
  if (saldoDepois.value < 0 || saldoDepois.value > produto.value.estoque_maximo) return 'mov__neg'
  if (saldoDepois.value <= produto.value.estoque_minimo) return 'mov__baixo'
  return 'mov__ok'
})

const passaDoSaldo = computed(() => props.tipo === 'saida' && saldoDepois.value !== null && saldoDepois.value < 0)
const passaDoMaximo = computed(
  () => props.tipo === 'entrada' && saldoDepois.value !== null && saldoDepois.value > produto.value.estoque_maximo
)
const ficaParaRepor = computed(
  () => props.tipo !== 'entrada' && saldoDepois.value !== null && saldoDepois.value >= 0 && saldoDepois.value <= produto.value.estoque_minimo
)
const ajusteIgualAoSaldo = computed(
  () => props.tipo === 'ajuste' && produto.value && quantidadeDigitada.value === produto.value.quantidade
)

function aoMudarCampo() {
  erros.value = {}
  resultado.value = null
  erroDaApi.value = ''
}

function conferirFormulario() {
  const encontrados = {}
  if (!produto.value) encontrados.codigo = 'Escolha o produto.'
  if (!quantidade.value.trim()) encontrados.quantidade = 'Informe a quantidade.'
  else if (quantidadeDigitada.value === null) encontrados.quantidade = 'Use um número inteiro.'
  else if (props.tipo !== 'ajuste' && quantidadeDigitada.value === 0) encontrados.quantidade = 'Use um número maior que zero.'
  if (props.tipo === 'entrada' && valorUnitario.value.trim() && numeroDecimal(valorUnitario.value) === null) {
    encontrados.valorUnitario = 'Use um valor como 12,50.'
  }
  if (props.tipo === 'ajuste' && !observacao.value.trim()) encontrados.observacao = 'Explique o motivo do ajuste.'
  return encontrados
}

function enviarParaApi() {
  const observacaoLimpa = observacao.value.trim() || undefined
  if (props.tipo === 'entrada') {
    const dados = { codigo: codigo.value, quantidade: quantidadeDigitada.value, observacao: observacaoLimpa }
    if (valorUnitario.value.trim()) dados.valor_unitario = numeroDecimal(valorUnitario.value)
    return registrarEntrada(dados)
  }
  if (props.tipo === 'saida') {
    return registrarSaida({ codigo: codigo.value, quantidade: quantidadeDigitada.value, observacao: observacaoLimpa })
  }
  return registrarAjuste({ codigo: codigo.value, quantidade_contada: quantidadeDigitada.value, observacao: observacaoLimpa })
}

async function registrar() {
  erros.value = conferirFormulario()
  resultado.value = null
  erroDaApi.value = ''
  if (Object.keys(erros.value).length > 0) return

  registrando.value = true
  try {
    resultado.value = await enviarParaApi()
    quantidade.value = ''
    valorUnitario.value = ''
    observacao.value = ''
    await carregar()
    atualizarAlertas()
  } catch (falha) {
    erroDaApi.value = falha.message
  } finally {
    registrando.value = false
  }
}
</script>

<template>
  <div class="duas-colunas">
    <form class="cadastro__cartao" novalidate @submit.prevent="registrar">
      <header :class="['cadastro__cabecalho', 'mov__cabecalho', `mov__cabecalho--${tipo}`]">
        <span class="mov__icone"><IconeSvg :nome="tipo" :tamanho="22" /></span>
        <div>
          <h2 class="t-h3">{{ texto.titulo }}</h2>
          <p class="t-small cadastro__subtitulo">{{ texto.explicacao }}</p>
        </div>
      </header>

      <div class="cadastro__corpo">
        <AlertaAviso v-if="resultado" tom="ok">
          {{ resultado.movimentacao.tipo_descricao }} registrada em <strong>{{ resultado.produto.nome }}</strong>.
          Saldo agora: <strong>{{ formatarNumero(resultado.produto.quantidade) }}</strong>.
          <template v-for="alerta in resultado.alertas" :key="alerta"><br />Atenção: {{ alerta }}.</template>
        </AlertaAviso>

        <AlertaAviso v-if="erroDaApi" tom="erro">{{ erroDaApi }}</AlertaAviso>

        <CampoFormulario v-model="codigo" rotulo="Produto" tipo-de-campo="lista" :erro="erros.codigo" @change="aoMudarCampo">
          <option value="">Selecione o produto</option>
          <option v-for="item in produtos" :key="item.codigo" :value="item.codigo">
            {{ item.codigo }} — {{ item.nome }} (saldo {{ formatarNumero(item.quantidade) }})
          </option>
        </CampoFormulario>

        <template v-if="produto">
          <div class="mov__produto">
            <IconeCorredor :corredor="produto.endereco.corredor" :tamanho="44" />
            <div>
              <strong class="t-smallm">{{ produto.nome }}</strong>
              <span class="t-label">{{ produto.codigo }} · {{ produto.endereco.descricao }}</span>
            </div>
          </div>

          <div class="mov__saldo">
            <div>
              <span class="t-label fraco">Saldo atual</span>
              <strong>{{ formatarNumero(produto.quantidade) }}</strong>
            </div>
            <div>
              <span class="t-label fraco">Mín. / máx.</span>
              <strong>{{ formatarNumero(produto.estoque_minimo) }} / {{ formatarNumero(produto.estoque_maximo) }}</strong>
            </div>
            <div>
              <span class="t-label fraco">Saldo após</span>
              <strong :class="corDoSaldoDepois">{{ saldoDepois === null ? '—' : formatarNumero(saldoDepois) }}</strong>
            </div>
          </div>
        </template>

        <div :class="{ 'grade-dupla': tipo === 'entrada' }">
          <CampoFormulario
            v-model="quantidade"
            :rotulo="texto.rotuloDaQuantidade"
            inputmode="numeric"
            placeholder="0"
            maxlength="8"
            :erro="erros.quantidade"
            @input="aoMudarCampo"
          />
          <CampoFormulario
            v-if="tipo === 'entrada'"
            v-model="valorUnitario"
            rotulo="Custo unitário desta compra"
            opcional
            inputmode="decimal"
            placeholder="0,00"
            sufixo="R$"
            :erro="erros.valorUnitario"
            :ajuda="produto ? `Hoje: ${formatarDinheiro(produto.valor_unitario)}. Se informar, vira o custo médio.` : 'Se informar, vira o custo médio.'"
            @input="aoMudarCampo"
          />
        </div>

        <AlertaAviso v-if="passaDoSaldo" tom="erro">
          A saída é maior que o saldo ({{ formatarNumero(produto.quantidade) }}). O estoque não pode ficar negativo.
        </AlertaAviso>
        <AlertaAviso v-if="passaDoMaximo" tom="erro">
          Com essa entrada o produto passa do estoque máximo ({{ formatarNumero(produto.estoque_maximo) }}).
          Cabem só mais {{ formatarNumero(Math.max(0, produto.estoque_maximo - produto.quantidade)) }}.
        </AlertaAviso>
        <AlertaAviso v-if="ficaParaRepor" tom="info">
          Depois disso o produto fica <strong>no mínimo ou abaixo</strong> e entra na lista de reposição.
        </AlertaAviso>
        <AlertaAviso v-if="ajusteIgualAoSaldo" tom="info">A quantidade contada é igual ao saldo. Não há o que ajustar.</AlertaAviso>

        <CampoFormulario
          v-model="observacao"
          :rotulo="texto.rotuloDaObservacao"
          :opcional="tipo !== 'ajuste'"
          :placeholder="texto.exemploDaObservacao"
          maxlength="200"
          :erro="erros.observacao"
          @input="aoMudarCampo"
        />
      </div>

      <footer class="cadastro__rodape">
        <span class="t-small cadastro__obrigatorios pessoa-cel">
          <AvatarPessoa :nome="usuario?.nome ?? ''" :tamanho="26" />
          Responsável: {{ usuario?.nome }}
        </span>
        <BotaoBase tipo="submit" alto :icone="tipo" :carregando="registrando">
          {{ registrando ? 'Registrando…' : texto.textoDoBotao }}
        </BotaoBase>
      </footer>
    </form>

    <section class="cartao">
      <header class="cartao__cabecalho">
        <div class="cartao__titulo">
          <span :class="['tipo-cel__seta', 'tipo-cel__seta--grande', `tipo-cel__seta--${tipo}`]">
            <IconeSvg :nome="tipo" :tamanho="18" />
          </span>
          <h2 class="t-h3">{{ texto.tituloDaLista }}</h2>
        </div>
        <RouterLink class="link" :to="{ path: '/movimentacoes', query: { tipo: texto.tipoDaApi } }">Ver todas</RouterLink>
      </header>
      <div class="tabela-rolagem">
        <table class="tabela">
          <thead>
            <tr>
              <th>Data</th>
              <th>Produto</th>
              <th class="num">Qtd.</th>
              <th class="num">Saldo após</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="movimentacao in ultimas" :key="movimentacao.id">
              <td class="fraco sem-quebra">{{ formatarDataHora(movimentacao.data_hora) }}</td>
              <td>
                <CelulaProduto
                  :nome="movimentacao.produto_nome"
                  :codigo="movimentacao.produto_codigo"
                  :detalhe="movimentacao.observacao || movimentacao.produto_codigo"
                />
              </td>
              <td :class="['num', `qtd--${tipo}`]">{{ quantidadeComSinal(movimentacao) }}</td>
              <td class="num">{{ formatarNumero(movimentacao.saldo_atual) }}</td>
            </tr>
            <tr v-if="ultimas.length === 0">
              <td colspan="4" class="tabela__vazia">Nenhum registro ainda.</td>
            </tr>
          </tbody>
        </table>
      </div>
    </section>
  </div>
</template>
