<script setup>
import IconeSvg from './IconeSvg.vue'

defineProps({
  variante: { type: String, default: 'primario' },
  icone: { type: String, default: '' },
  carregando: { type: Boolean, default: false },
  alto: { type: Boolean, default: false },
  tipo: { type: String, default: 'button' },
  desativado: { type: Boolean, default: false }
})
</script>

<template>
  <button
    :type="tipo"
    :class="['btn', `btn--${variante}`, { 'btn--alto': alto }]"
    :disabled="carregando || desativado"
  >
    <span v-if="carregando" class="btn__spinner" />
    <IconeSvg v-else-if="icone" :nome="icone" :tamanho="18" />
    <slot />
  </button>
</template>
