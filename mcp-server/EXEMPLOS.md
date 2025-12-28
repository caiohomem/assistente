# Exemplos de Uso do Servidor MCP

Aqui estão exemplos práticos de como usar o servidor MCP através do Cursor.

## 📝 Exemplos de Comandos

### Contatos

#### Criar um Contato
```
Crie um novo contato com os seguintes dados:
- Nome: João Silva
- Sobrenome: Silva
- Empresa: Tech Solutions
- Cargo: Desenvolvedor Senior
- Email: joao@techsolutions.com
- Telefone: (11) 98765-4321
```

#### Listar Contatos
```
Liste todos os meus contatos
```

```
Mostre meus contatos, página 1, 20 por página
```

#### Buscar Contatos
```
Busque contatos da empresa "Tech Solutions"
```

```
Encontre contatos com o nome "João"
```

#### Obter Detalhes de um Contato
```
Mostre os detalhes do contato com ID [cole-o-id-aqui]
```

#### Atualizar Contato
```
Atualize o contato [id] com o novo cargo "Tech Lead"
```

#### Adicionar Email/Telefone
```
Adicione o email joao.pessoal@gmail.com ao contato [id]
```

```
Adicione o telefone (11) 99999-8888 ao contato [id]
```

### Lembretes

#### Criar Lembrete
```
Crie um lembrete para entrar em contato com o João Silva amanhã às 14h.
Motivo: Follow-up sobre proposta comercial
```

```
Agende um lembrete para o contato [id] no dia 25/12/2024 às 10:00.
Motivo: Reunião de apresentação
Mensagem sugerida: "Olá, gostaria de agendar uma reunião para apresentar nossos produtos"
```

#### Listar Lembretes
```
Mostre meus lembretes pendentes
```

```
Liste todos os lembretes do contato [id]
```

```
Mostre meus lembretes agendados para esta semana
```

#### Atualizar Status do Lembrete
```
Marque o lembrete [id] como concluído
```

```
Cancele o lembrete [id]
```

### Notas

#### Criar Nota
```
Adicione uma nota ao contato [id]: "Cliente interessado em nosso produto X. Demonstrou interesse em agendar uma demo."
```

```
Crie uma nota para o contato [id] com o seguinte conteúdo:
"Reunião realizada em 20/12/2024. Cliente solicitou orçamento para 100 licenças.
Prazo de resposta: 30 dias. Valor estimado: R$ 50.000,00"
```

#### Listar Notas
```
Mostre todas as notas do contato [id]
```

#### Atualizar Nota
```
Atualize a nota [id] com o seguinte conteúdo: [novo conteúdo]
```

### Créditos

#### Verificar Saldo
```
Quantos créditos eu tenho disponíveis?
```

```
Mostre meu saldo de créditos
```

#### Listar Transações
```
Mostre minhas transações de crédito dos últimos 30 dias
```

#### Listar Pacotes
```
Quais pacotes de créditos estão disponíveis?
```

#### Comprar Pacote
```
Compre o pacote de créditos com ID [package-id]
```

### Automação

#### Criar Draft
```
Crie um draft de email para o contato [id] com o seguinte conteúdo:
"Prezado [Nome], gostaríamos de apresentar nossa solução..."
```

#### Listar Drafts
```
Mostre meus drafts pendentes
```

#### Aprovar e Enviar Draft
```
Aprove o draft [id]
```

```
Envie o draft [id]
```

### Consultas Combinadas

#### Resumo de Contato
```
Mostre um resumo completo do contato [id], incluindo:
- Informações básicas
- Todas as notas
- Lembretes pendentes
- Relacionamentos
```

#### Dashboard Rápido
```
Dê-me um resumo rápido:
- Quantos contatos eu tenho?
- Quantos lembretes pendentes?
- Qual meu saldo de créditos?
```

## 💡 Dicas de Uso

### 1. Use IDs quando possível
Se você já tem o ID de um contato, use-o diretamente:
```
Mostre o contato com ID 123e4567-e89b-12d3-a456-426614174000
```

### 2. Combine múltiplas ações
```
Crie um contato chamado Maria Santos da empresa ABC e depois crie um lembrete para entrar em contato com ela amanhã
```

### 3. Use filtros
```
Mostre apenas meus lembretes pendentes agendados para esta semana
```

### 4. Formate datas corretamente
Para lembretes, use formato ISO 8601:
```
2024-12-25T10:00:00Z
```

Ou deixe o Cursor interpretar:
```
amanhã às 10h
próxima segunda-feira às 14:00
```

## 🎯 Casos de Uso Reais

### Caso 1: Novo Lead
```
1. Crie um contato para o novo lead:
   - Nome: Pedro Alves
   - Empresa: StartupXYZ
   - Email: pedro@startupxyz.com
   - Cargo: CEO

2. Adicione uma nota: "Lead qualificado através de evento. Interessado em solução enterprise."

3. Crie um lembrete para entrar em contato amanhã às 9h com o motivo "Apresentar proposta comercial"
```

### Caso 2: Follow-up de Vendas
```
1. Busque contatos da empresa "Tech Corp"
2. Para cada contato encontrado, crie um lembrete para próxima semana com o motivo "Follow-up pós-apresentação"
```

### Caso 3: Organização de Contatos
```
1. Liste todos os meus contatos
2. Para cada contato sem email, adicione uma nota solicitando o email
3. Crie lembretes para solicitar emails faltantes
```

## 🔍 Buscas Avançadas

### Buscar por Múltiplos Critérios
```
Busque contatos que contenham "Tech" no nome ou empresa
```

### Filtrar por Data
```
Mostre lembretes agendados entre 01/01/2024 e 31/01/2024
```

### Combinar Filtros
```
Liste meus contatos da empresa "ABC" que tenham lembretes pendentes
```

## ⚠️ Limitações

- Tokens expiram em 1 hora - renove quando necessário
- Algumas operações requerem IDs válidos
- Paginação: use `page` e `pageSize` para grandes listas

## 🚀 Próximos Passos

Experimente fazer suas próprias consultas! O Cursor entenderá seus comandos em português e usará as ferramentas MCP apropriadas.





