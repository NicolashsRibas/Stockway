<script setup>
import IconeSvg from './IconeSvg.vue'

defineProps({
  tom: { type: String, default: 'erro' }
})
</script>

<template>
  <div :class="['alerta', `alerta--${tom}`]" :role="tom === 'erro' ? 'alert' : 'status'">
    <IconeSvg :nome="tom === 'ok' ? 'certo' : 'alerta'" :tamanho="18" />
    <span class="alerta__texto t-small"><slot /></span>
    <slot name="acao" />
  </div>
</template>
