<script setup>
import AvatarPessoa from './AvatarPessoa.vue'

defineProps({
  nome: { type: String, default: '' }
})
</script>

<template>
  <span class="pessoa-cel">
    <AvatarPessoa :nome="nome" :tamanho="26" />
    {{ nome }}
  </span>
</template>
