<script setup>
import { ref, nextTick } from 'vue'
import IconeSvg from '../components/IconeSvg.vue'
import BotaoBase from '../components/BotaoBase.vue'
import AlertaAviso from '../components/AlertaAviso.vue'
import FormularioProduto from '../components/FormularioProduto.vue'
import { cadastrarProduto } from '../services/produtosService'
import { atualizarAlertas } from '../stores/alertas'
import { formularioVazio, validarProduto, montarProdutoNovo } from '../utils/validarProduto'
import { formatarDinheiro } from '../utils/formatar'

const formulario = ref(formularioVazio())
const erros = ref({})
const jaTentouSalvar = ref(false)
const salvando = ref(false)
const erroDaApi = ref('')
const produtoSalvo = ref(null)

function aoMudarCampo() {
  produtoSalvo.value = null
  erroDaApi.value = ''
  if (jaTentouSalvar.value) erros.value = validarProduto(formulario.value)
}

async function focarPrimeiroErro() {
  await nextTick()
  document.querySelector('.cadastro [aria-invalid="true"]')?.focus()
}

async function salvar() {
  jaTentouSalvar.value = true
  erroDaApi.value = ''
  produtoSalvo.value = null

  erros.value = validarProduto(formulario.value)
  if (Object.keys(erros.value).length > 0) {
    focarPrimeiroErro()
    return
  }

  salvando.value = true
  try {
    produtoSalvo.value = await cadastrarProduto(montarProdutoNovo(formulario.value))
    formulario.value = formularioVazio()
    jaTentouSalvar.value = false
    atualizarAlertas()
    window.scrollTo({ top: 0, behavior: 'smooth' })
  } catch (falha) {
    erroDaApi.value = falha.message
    window.scrollTo({ top: 0, behavior: 'smooth' })
  } finally {
    salvando.value = false
  }
}

function limpar() {
  formulario.value = formularioVazio()
  erros.value = {}
  jaTentouSalvar.value = false
  erroDaApi.value = ''
  produtoSalvo.value = null
}
</script>

<template>
  <form class="cadastro" novalidate @submit.prevent="salvar" @input="aoMudarCampo" @change="aoMudarCampo">
    <AlertaAviso v-if="produtoSalvo" tom="ok">
      Produto <strong>{{ produtoSalvo.codigo }} — {{ produtoSalvo.nome }}</strong> cadastrado em
      {{ produtoSalvo.endereco.descricao }}, valendo {{ formatarDinheiro(produtoSalvo.valor_em_estoque) }} em estoque.
      <RouterLink class="link" :to="`/produtos/${encodeURIComponent(produtoSalvo.codigo)}`">Ver produto</RouterLink>
    </AlertaAviso>

    <AlertaAviso v-if="erroDaApi" tom="erro">{{ erroDaApi }}</AlertaAviso>

    <div class="cadastro__cartao">
      <header class="cadastro__cabecalho mov__cabecalho mov__cabecalho--saida">
        <span class="mov__icone"><IconeSvg nome="mais" :tamanho="22" /></span>
        <div>
          <h2 class="t-h3">Cadastrar produto</h2>
          <p class="t-small cadastro__subtitulo">Identificação, endereço, limites de estoque e valores do item.</p>
        </div>
      </header>

      <FormularioProduto v-model="formulario" :erros="erros" />

      <footer class="cadastro__rodape">
        <span class="t-small cadastro__obrigatorios">Campos sem a marca “opcional” são obrigatórios.</span>
        <div class="cadastro__acoes">
          <BotaoBase variante="secundario" alto :desativado="salvando" @click="limpar">Limpar</BotaoBase>
          <BotaoBase tipo="submit" alto icone="mais" :carregando="salvando">
            {{ salvando ? 'Salvando…' : 'Cadastrar produto' }}
          </BotaoBase>
        </div>
      </footer>
    </div>
  </form>
</template>
