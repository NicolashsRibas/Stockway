# StockWay — Front-end (Vue 3)

Telas do sistema de estoque e almoxarifado. Conversa com a API em C# que fica na raiz deste repositório.

## Como rodar

São dois terminais: um para a API e outro para o front.

**1. API (na raiz do repositório)**

```bash
dotnet run -- --exemplo
```

A API sobe em `http://localhost:8080`. O `--exemplo` cadastra 7 produtos para testar.

**2. Front (nesta pasta, `frontend/`)**

Precisa do [Node.js](https://nodejs.org) 20 ou mais novo.

```bash
npm install
npm run dev
```

Abra o endereço que aparecer no terminal (normalmente `http://localhost:5173`).

Se a API estiver em outro endereço, copie o `.env.example` para `.env`, ajuste o `VITE_API_URL` e rode o `npm run dev` de novo.

## Login

A API não tem login. A tela de entrada serve para saber **quem está usando**: o nome digitado vai como `responsavel` em todo cadastro, edição, entrada, saída e ajuste, e aparece no histórico.

## Como o código está organizado

```
src/
  views/        as telas (uma por arquivo)
  components/   pedaços que se repetem (botão, campo, selo, ícone, formulário de produto...)
  layouts/      a moldura do sistema: menu lateral e barra do topo
  router/       as rotas: qual endereço abre qual tela
  services/     o único lugar que conversa com a API
  stores/       informações que várias telas usam (quem está logado, quantos produtos para repor)
  utils/        funções de apoio (formatar R$, peso e data, validar o produto...)
  assets/css/   o visual: cores, tamanhos e estilos
```

### As telas

| Tela | Arquivo | Endereço | Rotas da API usadas |
|---|---|---|---|
| Entrada (identificação) | `LoginView.vue` | `#/login` | `GET /health` |
| Dashboard | `DashboardView.vue` | `#/dashboard` | `GET /dashboard` |
| Produtos | `ProdutosView.vue` | `#/produtos` | `GET /produtos` |
| Cadastrar produto | `ProdutoNovoView.vue` | `#/produtos/novo` | `POST /produtos` |
| Detalhes e edição | `ProdutoDetalheView.vue` | `#/produtos/:codigo` | `GET`, `PUT` e `DELETE /produtos/{codigo}`, `GET /produtos/{codigo}/movimentacoes` |
| Entradas | `MovimentarView.vue` | `#/entradas` | `POST /movimentacoes/entrada` |
| Saídas | `MovimentarView.vue` | `#/saidas` | `POST /movimentacoes/saida` |
| Ajuste de inventário | `MovimentarView.vue` | `#/ajustes` | `POST /movimentacoes/ajuste` |
| Movimentações | `MovimentacoesView.vue` | `#/movimentacoes` | `GET /movimentacoes` |

Entrada, saída e ajuste usam a mesma tela: o que muda é o `tipo`, definido na rota.

### Como uma tela pega os dados

As telas **nunca** chamam o `fetch` direto. Elas chamam uma função de `services/`:

```js
import { listarProdutos } from '../services/produtosService'

const resposta = await listarProdutos({ ordenar: 'nome' })
const produtos = resposta.itens
```

O `services/http.js` faz a chamada, abre o envelope `{ sucesso, dados, erro }` da API e, se der erro, lança a mensagem que a API mandou. As telas mostram essa mensagem num aviso vermelho.

### Validação

O front confere os campos antes de enviar, com as mesmas regras do C# (`utils/validarProduto.js`), para o usuário ver o erro embaixo do campo na hora. Mesmo assim, **quem decide é a API**: se ela recusar, a mensagem dela aparece no topo da tela.

## Scripts

| Comando | O que faz |
|---|---|
| `npm run dev` | roda o front para desenvolver |
| `npm run build` | gera a versão final na pasta `dist/` |
| `npm run preview` | abre a versão gerada pelo `build` |
