# Frontend Agenda

Este projeto é um front-end para gerenciamento de contatos, construído com Vue 3, Vite e PrimeVue. A aplicação permite cadastrar, visualizar, editar e excluir contatos de forma simples e intuitiva.

## Tecnologias Utilizadas

- [Vue 3](https://vuejs.org/)
- [Vite](https://vitejs.dev/)
- [PrimeVue](https://www.primefaces.org/primevue/)
- [TypeScript](https://www.typescriptlang.org/)
- [Vitest](https://vitest.dev/) – Testes unitários
- [Testing Library](https://testing-library.com/docs/vue-testing-library/intro/)

## Instalação

1. Clone o repositório:

```bash
git clone https://github.com/seu-usuario/frontend-agenda.git
cd frontend-agenda
```
2. Instale as dependências:
```bash
npm install
```
---
## Ambiente de Desenvolvimento

```bash
npm run dev
```
A aplicação estará disponível em: http://localhost:5173

---
## Scripts

npm run dev – Inicia o ambiente local de desenvolvimento
npm run build – Gera a versão de produção
npm run test – Executa os testes unitários com Vitest

---
## Estrutura de Pastas

```bash
src/
├── components/             # Componentes reutilizáveis
│   ├── __tests__/          # Testes unitários
│   └── *.vue
├── composables/            # Hooks reutilizáveis
├── router/                 # Rotas da aplicação
├── services/               # Serviços de API
├── stores/                 # Gerenciamento de estado (Pinia)
├── views/                  # Páginas principais da aplicação
├── test-utils.ts           # Configuração global para testes
└── main.ts                 # Ponto de entrada
```
---
## Testes
Os testes estão localizados em src/components/__tests__ e seguem o padrão do @testing-library/vue.

Para rodar os testes:

```bash
npx vitest
npx vitest --ui
```
