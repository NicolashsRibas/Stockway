<script setup>
import { computed } from 'vue'
import { coresDoCorredor } from '../utils/corredores'

const props = defineProps({
  corredor: { type: String, default: '' },
  tamanho: { type: Number, default: 36 }
})

const cores = computed(() => coresDoCorredor(props.corredor))
</script>

<template>
  <span
    class="icone-cat icone-corredor"
    :title="`Corredor ${corredor}`"
    :style="{
      width: `${tamanho}px`,
      height: `${tamanho}px`,
      background: cores.fundo,
      color: cores.cor,
      fontSize: `${Math.round(tamanho * 0.42)}px`
    }"
  >
    {{ corredor || '?' }}
  </span>
</template>
