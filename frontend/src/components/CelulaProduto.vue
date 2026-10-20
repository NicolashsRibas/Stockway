<script setup>
import IconeCorredor from './IconeCorredor.vue'

defineProps({
  nome: { type: String, default: '' },
  codigo: { type: String, default: '' },
  corredor: { type: String, default: '' },
  detalhe: { type: String, default: '' },
  tamanhoDoIcone: { type: Number, default: 32 },
  comLink: { type: Boolean, default: true }
})
</script>

<template>
  <span class="produto-cel">
    <IconeCorredor v-if="corredor" :corredor="corredor" :tamanho="tamanhoDoIcone" />
    <span>
      <RouterLink v-if="comLink && codigo" class="produto-cel__nome" :to="`/produtos/${encodeURIComponent(codigo)}`">
        {{ nome }}
      </RouterLink>
      <span v-else class="produto-cel__nome">{{ nome }}</span>
      <span class="produto-cel__sub">{{ detalhe || codigo }}</span>
    </span>
  </span>
</template>
