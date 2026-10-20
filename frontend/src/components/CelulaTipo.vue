<script setup>
import { computed } from 'vue'
import IconeSvg from './IconeSvg.vue'
import { TIPOS_DE_MOVIMENTACAO } from '../utils/estoque'

const props = defineProps({
  tipo: { type: String, required: true }
})

const informacao = computed(() => TIPOS_DE_MOVIMENTACAO[props.tipo] ?? TIPOS_DE_MOVIMENTACAO.EDICAO)
</script>

<template>
  <span class="tipo-cel">
    <span :class="['tipo-cel__seta', `tipo-cel__seta--${informacao.classe}`]">
      <IconeSvg :nome="informacao.icone" :tamanho="15" />
    </span>
    {{ informacao.rotulo }}
  </span>
</template>
