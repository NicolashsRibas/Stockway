<script setup>
import { ref, computed, onMounted } from 'vue'
import IconeSvg from '../components/IconeSvg.vue'
import IconeCorredor from '../components/IconeCorredor.vue'
import CelulaTipo from '../components/CelulaTipo.vue'
import CelulaProduto from '../components/CelulaProduto.vue'
import CelulaPessoa from '../components/CelulaPessoa.vue'
import SeloValidade from '../components/SeloValidade.vue'
import AlertaAviso from '../components/AlertaAviso.vue'
import { carregarDashboard } from '../services/estoqueService'
import { usuario } from '../stores/sessao'
import { quantidadeComSinal, TIPOS_DE_MOVIMENTACAO } from '../utils/estoque'
import { coresDoCorredor } from '../utils/corredores'
import {
  formatarNumero,
  formatarDinheiro,
  formatarPeso,
  formatarDataHora,
  formatarData,
  primeiroNome
} from '../utils/formatar'

const DIAS_DA_SEMANA = ['dom', 'seg', 'ter', 'qua', 'qui', 'sex', 'sáb']

const painel = ref(null)
const erro = ref('')

onMounted(async () => {
  try {
    painel.value = await carregarDashboard()
  } catch (falha) {
    erro.value = falha.message
  }
})

const saudacao = computed(() => {
  const hora = new Date().getHours()
  if (hora < 12) return 'Bom dia'
  if (hora < 18) return 'Boa tarde'
  return 'Boa noite'
})

const resumo = computed(() => painel.value.resumo)
const porStatus = computed(() => painel.value.estoque_por_status)
const produtosEmDia = computed(() => porStatus.value.normal + porStatus.value.acima_maximo)
const porcentagemEmDia = computed(() =>
  resumo.value.total_produtos ? Math.round((produtosEmDia.value / resumo.value.total_produtos) * 100) : 100
)

const semana = computed(() => painel.value.movimentacoes_semana)
const maiorValorDaSemana = computed(() => Math.max(1, ...semana.value.flatMap((dia) => [dia.entradas, dia.saidas])))
const unidadesDaSemana = computed(() => semana.value.reduce((total, dia) => total + dia.entradas + dia.saidas, 0))

const corredores = computed(() => painel.value.estoque_por_corredor)
const maiorCorredor = computed(() => Math.max(1, ...corredores.value.map((corredor) => corredor.unidades)))

function porcentagemDoMinimo(produto) {
  if (!produto.estoque_minimo) return 0
  return Math.min(100, Math.round((produto.quantidade / produto.estoque_minimo) * 100))
}

function rotuloDoDia(item, indice) {
  if (indice === semana.value.length - 1) return 'hoje'
  const [ano, mes, dia] = item.data.split('-').map(Number)
  const data = new Date(ano, mes - 1, dia)
  return `${DIAS_DA_SEMANA[data.getDay()]} ${data.getDate()}`
}
</script>

<template>
  <div class="pagina">
    <AlertaAviso v-if="erro" tom="erro">{{ erro }}</AlertaAviso>
    <p v-else-if="!painel" class="carregando-tela">Carregando o estoque…</p>

    <template v-else>
      <section class="dash__hero">
        <div class="dash__hero-texto">
          <span class="dash__hero-tag"><IconeSvg nome="brilho" :tamanho="14" /> Painel do estoque</span>
          <h2 class="t-h2">{{ saudacao }}, {{ primeiroNome(usuario?.nome) || 'equipe' }}!</h2>
          <p v-if="resumo.produtos_baixo_estoque > 0" class="t-body">
            Há <strong>{{ resumo.produtos_baixo_estoque }} produto{{ resumo.produtos_baixo_estoque > 1 ? 's' : '' }}</strong>
            para repor. Comece pela lista de reposição.
          </p>
          <p v-else class="t-body">Todo o estoque está acima do mínimo. Nada para repor agora.</p>
          <div class="dash__hero-acoes">
            <RouterLink class="dash__hero-btn dash__hero-btn--claro" to="/entradas">
              <IconeSvg nome="entrada" :tamanho="18" /> Registrar entrada
            </RouterLink>
            <RouterLink class="dash__hero-btn" to="/saidas"><IconeSvg nome="saida" :tamanho="18" /> Registrar saída</RouterLink>
            <RouterLink class="dash__hero-btn" to="/produtos/novo"><IconeSvg nome="mais" :tamanho="18" /> Novo produto</RouterLink>
          </div>
        </div>

        <div class="dash__saude" :aria-label="`${porcentagemEmDia}% dos produtos acima do mínimo`">
          <div class="dash__anel" :style="{ '--pct': porcentagemEmDia }">
            <div>
              <strong>{{ porcentagemEmDia }}%</strong>
              <span>em dia</span>
            </div>
          </div>
          <p class="t-small">{{ produtosEmDia }} de {{ resumo.total_produtos }} produtos acima do mínimo</p>
        </div>
      </section>

      <div class="resumo">
        <RouterLink class="resumo__card resumo__card--azul" to="/produtos">
          <div class="resumo__topo">
            <span class="resumo__icone"><IconeSvg nome="caixa" /></span>
            <IconeSvg nome="seta-direita" :tamanho="18" cor="var(--neutral-400)" />
          </div>
          <span class="t-small resumo__rotulo">Produtos cadastrados</span>
          <span class="resumo__valor">{{ formatarNumero(resumo.total_produtos) }}</span>
          <span class="resumo__apoio"><strong>{{ formatarNumero(resumo.total_unidades) }}</strong> unidades no estoque</span>
        </RouterLink>

        <RouterLink class="resumo__card resumo__card--verde" :to="{ path: '/produtos', query: { ordenar: 'valor_em_estoque' } }">
          <div class="resumo__topo">
            <span class="resumo__icone"><IconeSvg nome="dinheiro" /></span>
            <IconeSvg nome="seta-direita" :tamanho="18" cor="var(--neutral-400)" />
          </div>
          <span class="t-small resumo__rotulo">Valor em estoque</span>
          <span class="resumo__valor resumo__valor--dinheiro">{{ formatarDinheiro(resumo.valor_total_estoque) }}</span>
          <span class="resumo__apoio"><strong>{{ formatarPeso(resumo.peso_total_kg) }}</strong> no total</span>
        </RouterLink>

        <RouterLink class="resumo__card resumo__card--baixo" :to="{ path: '/produtos', query: { status: 'repor' } }">
          <div class="resumo__topo">
            <span class="resumo__icone"><IconeSvg nome="alerta" /></span>
            <IconeSvg nome="seta-direita" :tamanho="18" cor="var(--neutral-400)" />
          </div>
          <span class="t-small resumo__rotulo">Itens para repor</span>
          <span class="resumo__valor">{{ formatarNumero(resumo.produtos_baixo_estoque) }}</span>
          <span class="resumo__apoio">
            <strong>{{ porStatus.zerado }}</strong> zerado{{ porStatus.zerado === 1 ? '' : 's' }} · ver lista de reposição
          </span>
        </RouterLink>

        <RouterLink class="resumo__card resumo__card--violeta" :to="{ path: '/produtos', query: { validade: 'VENCE_EM_BREVE' } }">
          <div class="resumo__topo">
            <span class="resumo__icone"><IconeSvg nome="relogio" /></span>
            <IconeSvg nome="seta-direita" :tamanho="18" cor="var(--neutral-400)" />
          </div>
          <span class="t-small resumo__rotulo">Validade em alerta</span>
          <span class="resumo__valor">{{ formatarNumero(resumo.produtos_vencendo_em_breve + resumo.produtos_vencidos) }}</span>
          <span class="resumo__apoio">
            <strong>{{ resumo.produtos_vencidos }}</strong> vencido{{ resumo.produtos_vencidos === 1 ? '' : 's' }} ·
            {{ resumo.produtos_vencendo_em_breve }} vencendo em 30 dias
          </span>
        </RouterLink>
      </div>

      <div class="dash__grade">
        <section class="cartao">
          <header class="cartao__cabecalho">
            <div class="cartao__titulo">
              <span class="cartao__titulo-icone" style="background: var(--low-soft); color: var(--low)">
                <IconeSvg nome="alerta" :tamanho="18" />
              </span>
              <div>
                <h2 class="t-h3">Lista de reposição</h2>
                <p class="t-small cartao__sub">Zerados ou no mínimo, os mais críticos primeiro.</p>
              </div>
            </div>
            <RouterLink class="link" :to="{ path: '/produtos', query: { status: 'repor' } }">Ver todos</RouterLink>
          </header>

          <div v-if="painel.alertas_baixo_estoque.length === 0" class="dash__vazio">
            <span class="dash__vazio-icone"><IconeSvg nome="certo" :tamanho="28" /></span>
            <strong>Tudo em dia</strong>
            <span class="t-small">Nenhum produto abaixo do estoque mínimo.</span>
          </div>

          <ul v-else class="dash__baixos">
            <li v-for="produto in painel.alertas_baixo_estoque.slice(0, 6)" :key="produto.id">
              <IconeCorredor :corredor="produto.endereco.corredor" :tamanho="40" />
              <div class="dash__baixo-info">
                <div class="dash__baixo-topo">
                  <RouterLink class="dash__baixo-nome t-smallm" :to="`/produtos/${encodeURIComponent(produto.codigo)}`">
                    {{ produto.nome }}
                  </RouterLink>
                  <span class="t-small dash__baixo-qtd">
                    <strong>{{ formatarNumero(produto.quantidade) }}</strong> / {{ formatarNumero(produto.estoque_minimo) }} mín.
                  </span>
                </div>
                <div class="dash__barra" aria-hidden="true">
                  <span :style="{ width: `${Math.max(porcentagemDoMinimo(produto), 3)}%` }" />
                </div>
                <div class="dash__baixo-rodape t-label">
                  <span class="fraco">
                    {{ produto.codigo }} · {{ produto.endereco.descricao }} · repor {{ formatarNumero(produto.quantidade_para_repor) }}
                  </span>
                  <RouterLink class="dash__repor" :to="{ path: '/entradas', query: { produto: produto.codigo } }">
                    <IconeSvg nome="mais" :tamanho="12" /> Repor
                  </RouterLink>
                </div>
              </div>
            </li>
          </ul>
        </section>

        <div class="dash__coluna">
          <section class="cartao">
            <header class="cartao__cabecalho">
              <div class="cartao__titulo">
                <span class="cartao__titulo-icone" style="background: var(--brand-50); color: var(--brand-600)">
                  <IconeSvg nome="grafico" :tamanho="18" />
                </span>
                <div>
                  <h2 class="t-h3">Últimos 7 dias</h2>
                  <p class="t-small cartao__sub">{{ formatarNumero(unidadesDaSemana) }} unidades movimentadas</p>
                </div>
              </div>
              <div class="dash__legenda t-label">
                <span><i class="dash__cor dash__cor--entrada" /> Entradas</span>
                <span><i class="dash__cor dash__cor--saida" /> Saídas</span>
              </div>
            </header>

            <div class="dash__grafico" role="img" aria-label="Gráfico de unidades que entraram e saíram por dia">
              <div
                v-for="(item, indice) in semana"
                :key="item.data"
                :class="['dash__dia', { 'dash__dia--hoje': indice === semana.length - 1 }]"
              >
                <div class="dash__colunas">
                  <span
                    class="dash__col dash__col--entrada"
                    :style="{ height: `${(item.entradas / maiorValorDaSemana) * 100}%` }"
                    :title="`${item.entradas} unidades entraram`"
                  >
                    <em v-if="item.entradas > 0">{{ item.entradas }}</em>
                  </span>
                  <span
                    class="dash__col dash__col--saida"
                    :style="{ height: `${(item.saidas / maiorValorDaSemana) * 100}%` }"
                    :title="`${item.saidas} unidades saíram`"
                  >
                    <em v-if="item.saidas > 0">{{ item.saidas }}</em>
                  </span>
                </div>
                <span class="t-label dash__dia-rotulo">{{ rotuloDoDia(item, indice) }}</span>
              </div>
            </div>
          </section>

          <section class="cartao">
            <header class="cartao__cabecalho">
              <div class="cartao__titulo">
                <span class="cartao__titulo-icone" style="background: var(--accent-violet-soft); color: var(--accent-violet)">
                  <IconeSvg nome="grade" :tamanho="18" />
                </span>
                <div>
                  <h2 class="t-h3">Estoque por corredor</h2>
                  <p class="t-small cartao__sub">Unidades e valor guardados em cada corredor.</p>
                </div>
              </div>
            </header>
            <ul class="dash__cats">
              <li v-for="corredor in corredores" :key="corredor.corredor">
                <IconeCorredor :corredor="corredor.corredor" :tamanho="30" />
                <span class="dash__cat-nome t-smallm">{{ corredor.produtos }} produto{{ corredor.produtos === 1 ? '' : 's' }}</span>
                <span class="dash__cat-barra">
                  <span
                    :style="{
                      width: `${(corredor.unidades / maiorCorredor) * 100}%`,
                      background: coresDoCorredor(corredor.corredor).cor
                    }"
                  />
                </span>
                <span class="dash__cat-n t-small">
                  <strong>{{ formatarNumero(corredor.unidades) }}</strong> un. · {{ formatarDinheiro(corredor.valor) }}
                </span>
              </li>
            </ul>
          </section>
        </div>
      </div>

      <section v-if="painel.alertas_validade.length > 0" class="cartao">
        <header class="cartao__cabecalho">
          <div class="cartao__titulo">
            <span class="cartao__titulo-icone" style="background: var(--amber-soft); color: var(--amber)">
              <IconeSvg nome="relogio" :tamanho="18" />
            </span>
            <div>
              <h2 class="t-h3">Validade</h2>
              <p class="t-small cartao__sub">Produtos vencidos ou que vencem nos próximos 30 dias.</p>
            </div>
          </div>
        </header>
        <div class="tabela-rolagem">
          <table class="tabela">
            <thead>
              <tr>
                <th>Produto</th>
                <th>Validade</th>
                <th class="num">Quantidade</th>
                <th class="num">Valor em estoque</th>
                <th>Situação</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="produto in painel.alertas_validade" :key="produto.id">
                <td><CelulaProduto :nome="produto.nome" :codigo="produto.codigo" :corredor="produto.endereco.corredor" /></td>
                <td class="sem-quebra">{{ formatarData(produto.validade) }}</td>
                <td class="num">{{ formatarNumero(produto.quantidade) }}</td>
                <td class="num">{{ formatarDinheiro(produto.valor_em_estoque) }}</td>
                <td><SeloValidade :status="produto.status_validade" :dias="produto.dias_para_vencer" /></td>
              </tr>
            </tbody>
          </table>
        </div>
      </section>

      <section class="cartao">
        <header class="cartao__cabecalho">
          <div class="cartao__titulo">
            <span class="cartao__titulo-icone" style="background: var(--ok-soft); color: var(--ok)">
              <IconeSvg nome="historico" :tamanho="18" />
            </span>
            <h2 class="t-h3">Últimas movimentações</h2>
          </div>
          <RouterLink class="link" to="/movimentacoes">Ver histórico completo</RouterLink>
        </header>
        <div class="tabela-rolagem">
          <table class="tabela">
            <thead>
              <tr>
                <th>Tipo</th>
                <th>Produto</th>
                <th class="num">Quantidade</th>
                <th class="num">Saldo após</th>
                <th>Responsável</th>
                <th>Data</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="movimentacao in painel.ultimas_movimentacoes" :key="movimentacao.id">
                <td><CelulaTipo :tipo="movimentacao.tipo" /></td>
                <td><CelulaProduto :nome="movimentacao.produto_nome" :codigo="movimentacao.produto_codigo" /></td>
                <td :class="['num', `qtd--${TIPOS_DE_MOVIMENTACAO[movimentacao.tipo]?.classe}`]">
                  {{ quantidadeComSinal(movimentacao) }}
                </td>
                <td class="num">{{ formatarNumero(movimentacao.saldo_atual) }}</td>
                <td><CelulaPessoa :nome="movimentacao.responsavel" /></td>
                <td class="fraco sem-quebra">{{ formatarDataHora(movimentacao.data_hora) }}</td>
              </tr>
            </tbody>
          </table>
        </div>
      </section>
    </template>
  </div>
</template>
