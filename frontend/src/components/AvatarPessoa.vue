<script setup>
import { computed } from 'vue'

const props = defineProps({
  nome: { type: String, default: '' },
  tamanho: { type: Number, default: 28 }
})

const CORES = ['#2563EB', '#7C3AED', '#EA580C', '#0891B2', '#16A34A', '#DB2777']

const iniciais = computed(() => {
  const partes = props.nome.trim().split(/\s+/).filter(Boolean)
  if (partes.length === 0) return '?'
  const primeira = partes[0][0]
  const ultima = partes.length > 1 ? partes[partes.length - 1][0] : ''
  return (primeira + ultima).toUpperCase()
})

const cor = computed(() => {
  const soma = [...props.nome].reduce((total, letra) => total + letra.charCodeAt(0), 0)
  return CORES[soma % CORES.length]
})
</script>

<template>
  <span
    class="avatar"
    aria-hidden="true"
    :style="{ width: `${tamanho}px`, height: `${tamanho}px`, background: cor, fontSize: `${tamanho * 0.4}px` }"
  >
    {{ iniciais }}
  </span>
</template>
