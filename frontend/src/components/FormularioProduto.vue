<script setup>
import CampoFormulario from './CampoFormulario.vue'
import AlertaAviso from './AlertaAviso.vue'
import { computed } from 'vue'
import { LIMITES, numeroInteiro } from '../utils/validarProduto'
import { hojeNoFormatoDaApi } from '../utils/formatar'

const props = defineProps({
  erros: { type: Object, default: () => ({}) },
  cadastro: { type: Boolean, default: true }
})

const formulario = defineModel({ type: Object, required: true })

const nasceParaRepor = computed(() => {
  const quantidade = numeroInteiro(formulario.value.quantidade || '0')
  const minimo = numeroInteiro(formulario.value.estoqueMinimo)
  return props.cadastro && quantidade !== null && minimo !== null && quantidade <= minimo
})
</script>

<template>
  <div class="cadastro__corpo">
    <section class="cadastro__secao">
      <h3 class="t-label cadastro__secao-titulo"><span>1</span> Identificação</h3>
      <div class="cadastro__grade">
        <CampoFormulario
          v-model="formulario.codigo"
          rotulo="Código"
          placeholder="Ex.: LUV-020"
          :maxlength="LIMITES.codigo"
          :disabled="!cadastro"
          :erro="erros.codigo"
          :ajuda="cadastro ? 'Único e não pode ser mudado depois.' : 'O código não pode ser alterado.'"
        />
        <CampoFormulario
          v-model="formulario.nome"
          rotulo="Nome do produto"
          placeholder="Ex.: Luva de raspa (par)"
          :maxlength="LIMITES.nomeMaximo"
          :erro="erros.nome"
        />
      </div>
    </section>

    <section class="cadastro__secao">
      <h3 class="t-label cadastro__secao-titulo"><span>2</span> Endereço no almoxarifado</h3>
      <div class="cadastro__grade">
        <CampoFormulario
          v-model="formulario.corredor"
          rotulo="Corredor"
          placeholder="Ex.: B"
          :maxlength="LIMITES.endereco"
          :erro="erros.corredor"
        />
        <CampoFormulario
          v-model="formulario.prateleira"
          rotulo="Prateleira"
          placeholder="Ex.: 04"
          :maxlength="LIMITES.endereco"
          :erro="erros.prateleira"
        />
      </div>
    </section>

    <section class="cadastro__secao">
      <h3 class="t-label cadastro__secao-titulo"><span>3</span> Estoque</h3>
      <div :class="['cadastro__grade', { 'cadastro__grade--tres': cadastro }]">
        <CampoFormulario
          v-if="cadastro"
          v-model="formulario.quantidade"
          rotulo="Quantidade inicial"
          opcional
          inputmode="numeric"
          placeholder="0"
          maxlength="8"
          :erro="erros.quantidade"
        />
        <CampoFormulario
          v-model="formulario.estoqueMinimo"
          rotulo="Estoque mínimo"
          inputmode="numeric"
          placeholder="Ex.: 15"
          maxlength="8"
          :erro="erros.estoqueMinimo"
          ajuda="No mínimo ou abaixo, o produto entra na lista de reposição."
        />
        <CampoFormulario
          v-model="formulario.estoqueMaximo"
          rotulo="Estoque máximo"
          inputmode="numeric"
          placeholder="Ex.: 150"
          maxlength="8"
          :erro="erros.estoqueMaximo"
          ajuda="Uma entrada não pode passar deste número."
        />
        <div v-if="nasceParaRepor" class="cadastro__largura-total">
          <AlertaAviso tom="info">
            A quantidade inicial está no mínimo ou abaixo. O produto já vai aparecer na <strong>lista de reposição</strong>.
          </AlertaAviso>
        </div>
      </div>
    </section>

    <section class="cadastro__secao">
      <h3 class="t-label cadastro__secao-titulo"><span>4</span> Valor, peso e validade</h3>
      <div class="cadastro__grade cadastro__grade--tres">
        <CampoFormulario
          v-model="formulario.valorUnitario"
          rotulo="Valor unitário"
          inputmode="decimal"
          placeholder="0,00"
          sufixo="R$"
          :erro="erros.valorUnitario"
        />
        <CampoFormulario
          v-model="formulario.pesoKg"
          rotulo="Peso de uma unidade"
          inputmode="decimal"
          placeholder="0,000"
          sufixo="kg"
          :erro="erros.pesoKg"
        />
        <CampoFormulario
          v-model="formulario.validade"
          rotulo="Validade"
          opcional
          type="date"
          :min="hojeNoFormatoDaApi()"
          :erro="erros.validade"
          ajuda="Deixe em branco se o produto não vence."
        />
      </div>
    </section>
  </div>
</template>
