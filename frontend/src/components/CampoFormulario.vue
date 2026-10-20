<script setup>
import { computed, useId } from 'vue'
import IconeSvg from './IconeSvg.vue'

defineOptions({ inheritAttrs: false })

const props = defineProps({
  rotulo: { type: String, required: true },
  erro: { type: String, default: '' },
  ajuda: { type: String, default: '' },
  opcional: { type: Boolean, default: false },
  sufixo: { type: String, default: '' },
  contador: { type: String, default: '' },
  contadorPassou: { type: Boolean, default: false },
  tipoDeCampo: { type: String, default: 'texto' }
})

const valor = defineModel({ type: [String, Number], default: '' })

const id = useId()
const idMensagem = `${id}-mensagem`
const textoDoErro = computed(() => props.erro.trim())
const temMensagem = computed(() => Boolean(textoDoErro.value || props.ajuda))
</script>

<template>
  <div class="campo">
    <div class="campo__linha-label">
      <label class="campo__label t-smallm" :for="id">
        {{ rotulo }}
        <span v-if="opcional" class="campo__opcional"> (opcional)</span>
      </label>
      <span v-if="contador" :class="['campo__contador', 't-label', { 'campo__contador--estourou': contadorPassou }]">
        {{ contador }}
      </span>
    </div>

    <div :class="['campo__caixa', { 'campo__caixa--erro': erro, 'campo__caixa--area': tipoDeCampo === 'area' }]">
      <template v-if="tipoDeCampo === 'lista'">
        <select
          :id="id"
          v-model="valor"
          v-bind="$attrs"
          :aria-invalid="erro ? 'true' : undefined"
          :aria-describedby="temMensagem ? idMensagem : undefined"
        >
          <slot />
        </select>
        <IconeSvg nome="seta-baixo" :tamanho="18" cor="var(--neutral-400)" />
      </template>

      <textarea
        v-else-if="tipoDeCampo === 'area'"
        :id="id"
        v-model="valor"
        v-bind="$attrs"
        :aria-invalid="erro ? 'true' : undefined"
        :aria-describedby="temMensagem ? idMensagem : undefined"
      />

      <input
        v-else
        :id="id"
        v-model="valor"
        v-bind="$attrs"
        :aria-invalid="erro ? 'true' : undefined"
        :aria-describedby="temMensagem ? idMensagem : undefined"
      />

      <span v-if="sufixo && tipoDeCampo !== 'lista'" class="campo__sufixo t-small">{{ sufixo }}</span>
    </div>

    <p v-if="textoDoErro" :id="idMensagem" class="campo__mensagem t-label">
      <IconeSvg nome="alerta" :tamanho="14" />
      {{ textoDoErro }}
    </p>
    <p v-else-if="!erro && ajuda" :id="idMensagem" class="campo__ajuda t-label">{{ ajuda }}</p>
  </div>
</template>
