<script setup>
import IconeSvg from './IconeSvg.vue'

defineProps({
  totalDePaginas: { type: Number, required: true },
  mostrarNumeros: { type: Boolean, default: true }
})

const pagina = defineModel({ type: Number, required: true })
</script>

<template>
  <div class="paginacao">
    <button type="button" :disabled="pagina === 1" aria-label="Página anterior" @click="pagina--">
      <IconeSvg nome="seta-esquerda" :tamanho="16" />
    </button>
    <template v-if="mostrarNumeros">
      <button
        v-for="numero in totalDePaginas"
        :key="numero"
        type="button"
        :aria-current="numero === pagina ? 'page' : undefined"
        @click="pagina = numero"
      >
        {{ numero }}
      </button>
    </template>
    <span v-else class="t-small paginacao__texto">{{ pagina }} de {{ totalDePaginas }}</span>
    <button type="button" :disabled="pagina === totalDePaginas" aria-label="Próxima página" @click="pagina++">
      <IconeSvg nome="seta-direita" :tamanho="16" />
    </button>
  </div>
</template>
