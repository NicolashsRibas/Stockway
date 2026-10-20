<script setup>
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import IconeSvg from '../components/IconeSvg.vue'
import BotaoBase from '../components/BotaoBase.vue'
import CampoFormulario from '../components/CampoFormulario.vue'
import AlertaAviso from '../components/AlertaAviso.vue'
import { entrar } from '../stores/sessao'
import { verificarApi } from '../services/estoqueService'
import { ENDERECO_DA_API } from '../services/http'

const roteador = useRouter()

const nome = ref('')
const funcao = ref('')
const lembrar = ref(true)
const erro = ref('')
const situacaoDaApi = ref('verificando')

onMounted(async () => {
  try {
    await verificarApi()
    situacaoDaApi.value = 'ok'
  } catch {
    situacaoDaApi.value = 'fora'
  }
})

function fazerLogin() {
  const nomeLimpo = nome.value.trim()
  if (nomeLimpo.length < 2 || nomeLimpo.length > 60) {
    erro.value = 'Informe seu nome, com 2 a 60 letras.'
    return
  }
  entrar({ nome: nomeLimpo, funcao: funcao.value.trim() }, lembrar.value)
  roteador.push('/dashboard')
}
</script>
<template>
  <div class="login">
    <section class="login__marca">
      <div class="login__logo">
        <span class="login__logo-icone"><IconeSvg nome="caixa" :tamanho="24" /></span>
        <span class="t-h2">StockWay</span>
      </div>

      <div class="login__proposta">
        <h1 class="t-h1">Controle seu estoque com clareza.</h1>
        <p class="t-body login__sub">
          Produtos, quantidades e movimentações de entrada e saída em um só lugar, com aviso quando o estoque fica baixo.
        </p>
        <ul class="login__recursos">
          <li><span><IconeSvg nome="entrada" :tamanho="16" /></span> Entradas e saídas com saldo atualizado na hora</li>
          <li><span><IconeSvg nome="alerta" :tamanho="16" /></span> Alerta de estoque abaixo do mínimo</li>
          <li><span><IconeSvg nome="historico" :tamanho="16" /></span> Histórico completo de cada produto</li>
        </ul>

        <div class="login__preview" aria-hidden="true">
          <div class="login__preview-card">
            <span class="login__preview-icone login__preview-icone--verde"><IconeSvg nome="entrada" :tamanho="16" /></span>
            <div><strong>+50 UN</strong><span>Fita adesiva 48mm</span></div>
          </div>
          <div class="login__preview-card login__preview-card--2">
            <span class="login__preview-icone login__preview-icone--vermelho"><IconeSvg nome="alerta" :tamanho="16" /></span>
            <div><strong>6 itens</strong><span>para repor</span></div>
          </div>
        </div>
      </div>

      <span class="t-small login__segmentos">Varejo · Atacado · Logística</span>
    </section>

    <section class="login__painel">
      <form class="login__form" novalidate @submit.prevent="fazerLogin">
        <header>
          <h2 class="t-h2">Entrar</h2>
          <p class="t-small login__sub-form">Diga quem está usando o sistema. O seu nome fica registrado em cada movimentação.</p>
        </header>

        <AlertaAviso v-if="situacaoDaApi === 'fora'" tom="erro">
          A API não respondeu em {{ ENDERECO_DA_API }}. Ligue o back-end com <strong>dotnet run</strong> antes de usar o sistema.
        </AlertaAviso>

        <CampoFormulario
          v-model="nome"
          rotulo="Seu nome"
          autocomplete="name"
          placeholder="Ex.: Maria Souza"
          maxlength="60"
          :erro="erro"
          @input="erro = ''"
        />

        <CampoFormulario
          v-model="funcao"
          rotulo="Função"
          opcional
          placeholder="Ex.: Almoxarife"
          maxlength="40"
        />

        <label class="login__lembrar t-small">
          <input v-model="lembrar" type="checkbox" />
          Lembrar-me neste computador
        </label>

        <BotaoBase tipo="submit" alto>Entrar</BotaoBase>

        <p class="login__api t-label">
          <span :class="['login__api-ponto', `login__api-ponto--${situacaoDaApi}`]" />
          <template v-if="situacaoDaApi === 'ok'">API conectada</template>
          <template v-else-if="situacaoDaApi === 'fora'">API fora do ar</template>
          <template v-else>Verificando a API…</template>
        </p>
      </form>
    </section>
  </div>
</template>
