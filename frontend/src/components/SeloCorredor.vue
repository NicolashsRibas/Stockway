<script setup>
import { computed } from 'vue'
import { coresDoCorredor } from '../utils/corredores'

const props = defineProps({
  corredor: { type: String, default: '' },
  prateleira: { type: String, default: '' }
})

const cores = computed(() => coresDoCorredor(props.corredor))
</script>

<template>
  <span class="selo-cat" :style="{ background: cores.fundo, color: cores.cor }">
    Corredor {{ corredor }}<template v-if="prateleira"> · Prat. {{ prateleira }}</template>
  </span>
</template>
