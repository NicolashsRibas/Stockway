import { watch } from 'vue'
import { createRouter, createWebHashHistory } from 'vue-router'
import { usuario, avisoDeSaida, estaLogado, sair, MENSAGEM_SESSAO_EXPIRADA } from '../stores/sessao'
import LayoutPrincipal from '../layouts/LayoutPrincipal.vue'
import LoginView from '../views/LoginView.vue'
import DashboardView from '../views/DashboardView.vue'
import ProdutosView from '../views/ProdutosView.vue'
import ProdutoNovoView from '../views/ProdutoNovoView.vue'
import ProdutoDetalheView from '../views/ProdutoDetalheView.vue'
import MovimentarView from '../views/MovimentarView.vue'
import MovimentacoesView from '../views/MovimentacoesView.vue'

const rotas = [
  { path: '/login', name: 'login', component: LoginView, meta: { publica: true } },
  {
    path: '/',
    component: LayoutPrincipal,
    children: [
      { path: '', redirect: '/dashboard' },
      {
        path: 'dashboard',
        name: 'dashboard',
        component: DashboardView,
        meta: { titulo: 'Dashboard', subtitulo: 'Resumo visual do estoque', menu: 'dashboard', botaoCadastrar: true }
      },
      {
        path: 'produtos',
        name: 'produtos',
        component: ProdutosView,
        meta: { titulo: 'Estoque', subtitulo: 'Todos os produtos e o nível de cada um', menu: 'produtos', botaoCadastrar: true }
      },
      {
        path: 'produtos/novo',
        name: 'produto-novo',
        component: ProdutoNovoView,
        meta: { titulo: 'Produtos › Cadastrar', subtitulo: 'Novo item no almoxarifado', menu: 'produtos' }
      },
      {
        path: 'produtos/:codigo',
        name: 'produto-detalhe',
        component: ProdutoDetalheView,
        props: true,
        meta: { titulo: 'Produtos › Detalhes', subtitulo: 'Dados, saldo e histórico do produto', menu: 'produtos' }
      },
      {
        path: 'entradas',
        name: 'entradas',
        component: MovimentarView,
        props: { tipo: 'entrada' },
        meta: { titulo: 'Entradas', subtitulo: 'Material que chegou ao estoque', menu: 'entradas' }
      },
      {
        path: 'saidas',
        name: 'saidas',
        component: MovimentarView,
        props: { tipo: 'saida' },
        meta: { titulo: 'Saídas', subtitulo: 'Material que saiu do estoque', menu: 'saidas' }
      },
      {
        path: 'ajustes',
        name: 'ajustes',
        component: MovimentarView,
        props: { tipo: 'ajuste' },
        meta: { titulo: 'Ajuste de inventário', subtitulo: 'Corrige o saldo pela contagem física', menu: 'ajustes' }
      },
      {
        path: 'movimentacoes',
        name: 'movimentacoes',
        component: MovimentacoesView,
        meta: { titulo: 'Movimentações', subtitulo: 'Histórico de entradas e saídas', menu: 'movimentacoes' }
      }
    ]
  },
  { path: '/:caminho(.*)*', redirect: '/dashboard' }
]

const router = createRouter({
  history: createWebHashHistory(),
  routes: rotas,
  scrollBehavior: () => ({ top: 0 })
})

// Endereço para voltar depois de entrar (ex.: o link de um produto aberto antes de a sessão vencer).
function paraEntrar(caminho) {
  return caminho && caminho !== '/' && caminho !== '/dashboard' ? { name: 'login', query: { voltar: caminho } } : { name: 'login' }
}

router.beforeEach((destino) => {
  const logado = estaLogado()
  if (!logado && usuario.value) sair(MENSAGEM_SESSAO_EXPIRADA) // o token venceu com a aba aberta
  if (!destino.meta.publica && !logado) return paraEntrar(destino.fullPath)
  if (destino.name === 'login' && logado) return { name: 'dashboard' }
  return true
})

// Quando a sessão acaba (botão Sair ou token recusado pela API), volta para a tela de entrada.
watch(usuario, (atual) => {
  const rotaAtual = router.currentRoute.value
  if (atual || rotaAtual.meta.publica) return
  router.replace(avisoDeSaida.value ? paraEntrar(rotaAtual.fullPath) : { name: 'login' })
})

export default router
