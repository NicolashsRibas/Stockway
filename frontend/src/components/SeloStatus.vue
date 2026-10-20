<script setup>
import { computed } from 'vue'
import { STATUS_DO_ESTOQUE } from '../utils/estoque'

const props = defineProps({
  status: { type: String, required: true }
})

const informacao = computed(() => STATUS_DO_ESTOQUE[props.status] ?? STATUS_DO_ESTOQUE.NORMAL)
</script>

<template>
  <span :class="['selo', informacao.classe]">{{ informacao.rotulo }}</span>
</template>
