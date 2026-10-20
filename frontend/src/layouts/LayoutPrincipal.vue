<script setup>
import { ref, watch, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import IconeSvg from '../components/IconeSvg.vue'
import AvatarPessoa from '../components/AvatarPessoa.vue'
import BotaoBase from '../components/BotaoBase.vue'
import { usuario, sair as encerrarSessao } from '../stores/sessao'
import { quantidadeParaRepor, atualizarAlertas } from '../stores/alertas'
import { dataDeHoje } from '../utils/formatar'

const rota = useRoute()
const roteador = useRouter()

const GRUPOS_DO_MENU = [
  {
    titulo: 'Visão geral',
    itens: [
      { chave: 'dashboard', rotulo: 'Dashboard', icone: 'painel', destino: '/dashboard' },
      { chave: 'produtos', rotulo: 'Produtos', icone: 'caixa', destino: '/produtos', mostraAlerta: true }
    ]
  },
  {
    titulo: 'Operação',
    itens: [
      { chave: 'entradas', rotulo: 'Entradas', icone: 'entrada', destino: '/entradas' },
      { chave: 'saidas', rotulo: 'Saídas', icone: 'saida', destino: '/saidas' },
      { chave: 'ajustes', rotulo: 'Ajustes', icone: 'ajuste', destino: '/ajustes' },
      { chave: 'movimentacoes', rotulo: 'Movimentações', icone: 'historico', destino: '/movimentacoes' }
    ]
  }
]

const busca = ref('')

watch(
  () => rota.query.busca,
  (valor) => {
    busca.value = rota.name === 'produtos' ? valor ?? '' : ''
  },
  { immediate: true }
)

watch(() => rota.fullPath, atualizarAlertas)

onMounted(atualizarAlertas)

function pesquisar() {
  const termo = busca.value.trim()
  roteador.push({ path: '/produtos', query: termo ? { busca: termo } : {} })
}

function sair() {
  encerrarSessao()
  roteador.push('/login')
}
</script>

<template>
  <div class="shell">
    <aside class="shell__sidebar">
      <div>
        <RouterLink class="shell__marca" to="/dashboard">
          <span class="shell__logo"><IconeSvg nome="caixa" :tamanho="20" /></span>
          <span>
            <span class="t-h3">StockWay</span>
            <span class="shell__slogan">Estoque e almoxarifado</span>
          </span>
        </RouterLink>

        <nav class="shell__nav" aria-label="Navegação principal">
          <div v-for="grupo in GRUPOS_DO_MENU" :key="grupo.titulo" class="shell__grupo">
            <span class="shell__grupo-titulo">{{ grupo.titulo }}</span>
            <RouterLink
              v-for="item in grupo.itens"
              :key="item.chave"
              :to="item.destino"
              class="shell__nav-item t-smallm"
              :aria-current="rota.meta.menu === item.chave ? 'page' : undefined"
            >
              <IconeSvg :nome="item.icone" :tamanho="18" />
              <span class="shell__nav-rotulo">{{ item.rotulo }}</span>
              <span
                v-if="item.mostraAlerta && quantidadeParaRepor > 0"
                class="shell__contador"
                :title="`${quantidadeParaRepor} produtos para repor`"
              >
                {{ quantidadeParaRepor }}
              </span>
            </RouterLink>
          </div>
        </nav>
      </div>

      <div class="shell__rodape-lateral">
        <div class="shell__usuario">
          <AvatarPessoa :nome="usuario?.nome ?? ''" :tamanho="36" />
          <div class="shell__usuario-texto">
            <div class="t-smallm">{{ usuario?.nome }}</div>
            <div class="t-label shell__cargo">{{ usuario?.funcao || 'Responsável' }}</div>
          </div>
          <button type="button" class="shell__sair" title="Sair" aria-label="Sair" @click="sair">
            <IconeSvg nome="sair" :tamanho="18" />
          </button>
        </div>
      </div>
    </aside>

    <div class="shell__main">
      <header class="shell__topbar">
        <div class="shell__titulos">
          <h1 class="t-h3 shell__titulo">{{ rota.meta.titulo }}</h1>
          <p v-if="rota.meta.subtitulo" class="t-label shell__subtitulo">{{ rota.meta.subtitulo }}</p>
        </div>

        <form class="shell__busca" role="search" @submit.prevent="pesquisar">
          <IconeSvg nome="lupa" :tamanho="18" cor="var(--neutral-400)" />
          <input
            v-model="busca"
            type="search"
            placeholder="Pesquisar produto por nome ou código"
            aria-label="Pesquisar produto"
          />
        </form>

        <RouterLink
          class="shell__sino"
          :to="{ path: '/produtos', query: { status: 'repor' } }"
          title="Produtos para repor"
          :aria-label="`${quantidadeParaRepor} produtos para repor`"
        >
          <IconeSvg nome="sino" :tamanho="18" />
          <span v-if="quantidadeParaRepor > 0" class="shell__sino-ponto">{{ quantidadeParaRepor }}</span>
        </RouterLink>

        <BotaoBase v-if="rota.meta.botaoCadastrar" icone="mais" @click="roteador.push('/produtos/novo')">
          Cadastrar produto
        </BotaoBase>
      </header>

      <main :key="rota.path" class="shell__conteudo">
        <p class="shell__data t-label"><IconeSvg nome="calendario" :tamanho="14" /> {{ dataDeHoje() }}</p>
        <RouterView />
      </main>
    </div>
  </div>
</template>
