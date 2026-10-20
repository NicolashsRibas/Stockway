<script setup>
import { computed } from 'vue'
import { STATUS_DA_VALIDADE } from '../utils/estoque'

const props = defineProps({
  status: { type: String, required: true },
  dias: { type: Number, default: null }
})

const informacao = computed(() => STATUS_DA_VALIDADE[props.status] ?? STATUS_DA_VALIDADE.NAO_PERECIVEL)

const texto = computed(() => {
  if (props.status === 'VENCE_EM_BREVE') return props.dias === 0 ? 'Vence hoje' : `Vence em ${props.dias} dia${props.dias === 1 ? '' : 's'}`
  if (props.status === 'VENCIDO') return `Vencido há ${Math.abs(props.dias)} dia${Math.abs(props.dias) === 1 ? '' : 's'}`
  return informacao.value.rotulo
})
</script>

<template>
  <span :class="['selo', informacao.classe]">{{ texto }}</span>
</template>
