<script setup>
import { ref, computed, onMounted, nextTick } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import IconeSvg from '../components/IconeSvg.vue'
import BotaoBase from '../components/BotaoBase.vue'
import CampoFormulario from '../components/CampoFormulario.vue'
import AlertaAviso from '../components/AlertaAviso.vue'
import { entrar, avisoDeSaida } from '../stores/sessao'
import { verificarApi } from '../services/estoqueService'
import { fazerLogin, criarConta } from '../services/autenticacaoService'
import { ENDERECO_DA_API } from '../services/http'
import { LIMITES_DO_USUARIO, regrasDaSenha, validarLogin, validarCadastro } from '../utils/validarUsuario'

const CARGOS_SUGERIDOS = [
  'Almoxarife',
  'Auxiliar de almoxarifado',
  'Conferente',
  'Comprador',
  'Supervisor de estoque',
  'Gerente'
]

const TEXTOS = {
  entrar: {
    titulo: 'Entrar',
    explicacao: 'Use o e-mail e a senha da sua conta no StockWay.',
    botao: 'Entrar',
    pergunta: 'Ainda não tem conta?',
    troca: 'Criar conta'
  },
  criar: {
    titulo: 'Criar conta',
    explicacao: 'Seu nome e seu cargo ficam registrados em cada entrada, saída e ajuste que você fizer.',
    botao: 'Criar conta',
    pergunta: 'Já tem conta?',
    troca: 'Entrar'
  }
}

const roteador = useRouter()
const rota = useRoute()
const painel = ref(null)

const modo = ref('entrar')
const form = ref({ nome: '', cargo: '', email: '', senha: '' })
const lembrar = ref(false)
const mostrarSenha = ref(false)
const capsLock = ref(false)
const erros = ref({})
const erroDaApi = ref('')
const avisoDoFormulario = ref('')
const enviando = ref(false)
const situacaoDaApi = ref('verificando')

const criando = computed(() => modo.value === 'criar')
const texto = computed(() => TEXTOS[modo.value])
const regras = computed(() => regrasDaSenha(form.value.senha))
const aviso = computed(() => avisoDoFormulario.value || (!criando.value ? avisoDeSaida.value : ''))

onMounted(async () => {
  focarPrimeiroCampo()
  try {
    await verificarApi()
    situacaoDaApi.value = 'ok'
  } catch {
    situacaoDaApi.value = 'fora'
  }
})

function focarPrimeiroCampo(seletor = 'input') {
  nextTick(() => painel.value?.querySelector(seletor)?.focus())
}

function trocarModo() {
  modo.value = criando.value ? 'entrar' : 'criar'
  form.value.senha = ''
  mostrarSenha.value = false
  erros.value = {}
  erroDaApi.value = ''
  avisoDoFormulario.value = ''
  focarPrimeiroCampo()
}

function aoDigitar(campo) {
  if (erros.value[campo]) erros.value = { ...erros.value, [campo]: '' }
  erroDaApi.value = ''
}

function conferirCapsLock(evento) {
  capsLock.value = evento.getModifierState?.('CapsLock') ?? false
}

function destinoDepoisDeEntrar() {
  const voltar = rota.query.voltar
  return typeof voltar === 'string' && voltar.startsWith('/') && !voltar.startsWith('//') ? voltar : '/dashboard'
}

async function enviar() {
  erroDaApi.value = ''
  erros.value = criando.value ? validarCadastro(form.value) : validarLogin(form.value)
  if (Object.keys(erros.value).length > 0) {
    focarPrimeiroCampo('[aria-invalid="true"]')
    return
  }

  const email = form.value.email.trim()
  let contaCriada = false
  enviando.value = true
  try {
    if (criando.value) {
      await criarConta({ nome: form.value.nome.trim(), cargo: form.value.cargo.trim(), email, senha: form.value.senha })
      contaCriada = true
    }
    const resposta = await fazerLogin(email, form.value.senha)
    entrar(resposta, lembrar.value)
    roteador.replace(destinoDepoisDeEntrar())
  } catch (falha) {
    if (contaCriada) {
      // A conta existe, mas o login automático falhou (ex.: muitas tentativas): a pessoa entra pela outra tela.
      modo.value = 'entrar'
      avisoDoFormulario.value = 'Conta criada. Entre com o seu e-mail e a sua senha.'
      erroDaApi.value = falha.message
    } else if (criando.value && falha.status === 409) {
      erros.value = { email: falha.message }
      focarPrimeiroCampo('[aria-invalid="true"]')
    } else {
      erroDaApi.value = falha.message
    }
  } finally {
    enviando.value = false
  }
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

    <section ref="painel" class="login__painel">
      <form :key="modo" class="login__form" novalidate @submit.prevent="enviar">
        <header>
          <h2 class="t-h2">{{ texto.titulo }}</h2>
          <p class="t-small login__sub-form">{{ texto.explicacao }}</p>
        </header>

        <AlertaAviso v-if="aviso" tom="info">{{ aviso }}</AlertaAviso>
        <AlertaAviso v-if="situacaoDaApi === 'fora'" tom="erro">
          A API não respondeu em {{ ENDERECO_DA_API }}. Ligue o back-end com <strong>dotnet run</strong> antes de entrar.
        </AlertaAviso>
        <AlertaAviso v-if="erroDaApi" tom="erro">{{ erroDaApi }}</AlertaAviso>

        <template v-if="criando">
          <CampoFormulario
            v-model="form.nome"
            rotulo="Nome completo"
            autocomplete="name"
            placeholder="Ex.: Maria Souza"
            :maxlength="LIMITES_DO_USUARIO.nomeMaximo"
            :erro="erros.nome"
            @input="aoDigitar('nome')"
          />

          <CampoFormulario
            v-model="form.cargo"
            rotulo="Cargo"
            autocomplete="organization-title"
            placeholder="Ex.: Almoxarife"
            list="login-cargos"
            :maxlength="LIMITES_DO_USUARIO.cargoMaximo"
            :erro="erros.cargo"
            @input="aoDigitar('cargo')"
          />
          <datalist id="login-cargos">
            <option v-for="cargo in CARGOS_SUGERIDOS" :key="cargo" :value="cargo" />
          </datalist>
        </template>

        <CampoFormulario
          v-model="form.email"
          rotulo="E-mail"
          type="email"
          inputmode="email"
          :autocomplete="criando ? 'email' : 'username'"
          placeholder="nome@empresa.com.br"
          :maxlength="LIMITES_DO_USUARIO.emailMaximo"
          :erro="erros.email"
          @input="aoDigitar('email')"
        />

        <div class="login__senha">
          <CampoFormulario
            v-model="form.senha"
            rotulo="Senha"
            :type="mostrarSenha ? 'text' : 'password'"
            :autocomplete="criando ? 'new-password' : 'current-password'"
            :maxlength="LIMITES_DO_USUARIO.senhaMaxima"
            :erro="erros.senha"
            :ajuda="capsLock ? 'O Caps Lock está ativado.' : ''"
            @input="aoDigitar('senha')"
            @keydown="conferirCapsLock"
            @keyup="conferirCapsLock"
          />
          <button
            type="button"
            class="login__olho"
            :aria-label="mostrarSenha ? 'Ocultar senha' : 'Mostrar senha'"
            :aria-pressed="mostrarSenha ? 'true' : 'false'"
            @click="mostrarSenha = !mostrarSenha"
          >
            <IconeSvg :nome="mostrarSenha ? 'olho-fechado' : 'olho'" :tamanho="18" />
          </button>
        </div>

        <ul v-if="criando" class="login__regras t-label" aria-label="Regras da senha">
          <li v-for="regra in regras" :key="regra.texto" :class="{ 'login__regra--ok': regra.ok }">
            <span class="login__regra-marca"><IconeSvg v-if="regra.ok" nome="certo" :tamanho="10" /></span>
            <span class="sr-only">{{ regra.ok ? 'Cumprida:' : 'Falta:' }}</span>
            {{ regra.texto }}
          </li>
        </ul>

        <label class="login__lembrar t-small">
          <input v-model="lembrar" type="checkbox" />
          Manter conectado neste computador
        </label>

        <BotaoBase tipo="submit" alto :carregando="enviando">{{ texto.botao }}</BotaoBase>

        <p class="login__trocar t-small">
          {{ texto.pergunta }}
          <button type="button" class="link link--botao" @click="trocarModo">{{ texto.troca }}</button>
        </p>

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
