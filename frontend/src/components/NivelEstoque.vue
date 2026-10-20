<script setup>
import { computed } from 'vue'

const props = defineProps({
  quantidade: { type: Number, required: true },
  maximo: { type: Number, required: true },
  status: { type: String, required: true }
})

const largura = computed(() => {
  const porcentagem = props.maximo > 0 ? (props.quantidade / props.maximo) * 100 : 0
  return Math.max(4, Math.min(100, Math.round(porcentagem)))
})

const tom = computed(() => {
  if (props.status === 'ZERADO' || props.status === 'BAIXO') return 'baixo'
  if (props.status === 'ACIMA_MAXIMO') return 'atencao'
  return 'ok'
})
</script>

<template>
  <span :class="['nivel', `nivel--${tom}`]" :title="`${largura}% do estoque máximo`">
    <span :style="{ width: `${largura}%` }" />
  </span>
</template>
