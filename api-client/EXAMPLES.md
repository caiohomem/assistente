# Exemplos de Uso - Cliente API

## Exemplo 1: Listar e Criar Contatos

```typescript
import AssistenteExecutivoClient from "./src/index";

const client = new AssistenteExecutivoClient();

async function exemploContatos() {
  // Verificar autenticação
  const isAuth = await client.auth.isAuthenticated();
  if (!isAuth) {
    console.log("Faça login primeiro!");
    return;
  }

  // Listar contatos
  const contacts = await client.contacts.list({ page: 1, pageSize: 10 });
  console.log(`Total: ${contacts.totalCount}`);

  // Criar novo contato
  const novoContato = await client.contacts.create({
    firstName: "Maria",
    lastName: "Silva",
    company: "Empresa XYZ",
  });

  // Adicionar email
  await client.contacts.addEmail(novoContato.contactId, "maria@example.com");

  // Adicionar telefone
  await client.contacts.addPhone(novoContato.contactId, "+5511999999999");
}
```

## Exemplo 2: Gerenciar Lembretes

```typescript
import AssistenteExecutivoClient from "./src/index";
import { ReminderStatus } from "./src/types";

const client = new AssistenteExecutivoClient();

async function exemploLembretes() {
  // Criar lembrete
  const scheduledDate = new Date();
  scheduledDate.setDate(scheduledDate.getDate() + 7); // 7 dias

  const lembrete = await client.reminders.create({
    contactId: "contact-id",
    reason: "Seguir sobre proposta comercial",
    suggestedMessage: "Olá, gostaria de saber sobre a proposta...",
    scheduledFor: scheduledDate.toISOString(),
  });

  // Listar lembretes pendentes
  const pendentes = await client.reminders.list({
    status: ReminderStatus.Pending,
  });

  // Marcar como completo
  await client.reminders.updateStatus(lembrete.reminderId, {
    newStatus: ReminderStatus.Completed,
  });
}
```

## Exemplo 3: Trabalhar com Notas

```typescript
import AssistenteExecutivoClient from "./src/index";

const client = new AssistenteExecutivoClient();

async function exemploNotas() {
  const contactId = "contact-id";

  // Criar nota de texto
  const nota = await client.notes.createTextNote(contactId, {
    text: "Reunião importante marcada para próxima semana",
    structuredData: JSON.stringify({
      tipo: "reuniao",
      data: "2024-12-31",
    }),
  });

  // Listar todas as notas do contato
  const notas = await client.notes.listByContact(contactId);

  // Atualizar nota
  await client.notes.update(nota.noteId, {
    rawContent: "Reunião confirmada para 10h",
  });
}
```

## Exemplo 4: Automação (Drafts, Templates)

```typescript
import AssistenteExecutivoClient from "./src/index";
import { DocumentType, TemplateType } from "./src/types";

const client = new AssistenteExecutivoClient();

async function exemploAutomacao() {
  // Criar template
  const template = await client.automation.createTemplate({
    name: "Email de Apresentação",
    type: TemplateType.Email,
    body: "Olá {{nome}},\n\nGostaria de me apresentar...",
    placeholdersSchema: JSON.stringify({
      nome: "string",
      empresa: "string",
    }),
  });

  // Criar draft usando o template
  const draft = await client.automation.createDraft({
    documentType: DocumentType.Email,
    content: "Conteúdo do email...",
    templateId: template.templateId,
    contactId: "contact-id",
  });

  // Aprovar e enviar
  await client.automation.approveDraft(draft.draftId);
  await client.automation.sendDraft(draft.draftId);
}
```

## Exemplo 5: Gerenciar Créditos

```typescript
import AssistenteExecutivoClient from "./src/index";

const client = new AssistenteExecutivoClient();

async function exemploCreditos() {
  // Verificar saldo
  const balance = await client.credits.getBalance();
  console.log(`Saldo: ${balance.balance} créditos`);

  // Listar transações
  const transactions = await client.credits.getTransactions({
    limit: 10,
  });

  // Listar pacotes disponíveis
  const packages = await client.credits.listPackages();

  // Comprar pacote
  if (packages.length > 0) {
    await client.credits.purchasePackage(packages[0].packageId);
  }
}
```

## Exemplo 6: Busca e Filtros

```typescript
import AssistenteExecutivoClient from "./src/index";

const client = new AssistenteExecutivoClient();

async function exemploBusca() {
  // Buscar contatos
  const resultados = await client.contacts.search("João", {
    page: 1,
    pageSize: 20,
  });

  // Listar lembretes com filtros
  const lembretes = await client.reminders.list({
    contactId: "contact-id",
    status: ReminderStatus.Pending,
    startDate: new Date("2024-01-01"),
    endDate: new Date("2024-12-31"),
  });

  // Listar drafts filtrados
  const drafts = await client.automation.listDrafts({
    documentType: DocumentType.Letter,
    status: DraftStatus.Draft,
  });
}
```

## Exemplo 7: Upload de Arquivos

```typescript
import AssistenteExecutivoClient from "./src/index";

const client = new AssistenteExecutivoClient();

async function exemploUpload() {
  // Upload de cartão de visita (OCR)
  const cardResult = await client.capture.uploadCard("./cartao.jpg");
  console.log(`Contato criado: ${cardResult.contactId}`);

  // Processar nota de áudio
  const audioResult = await client.capture.processAudioNote(
    "contact-id",
    "./audio.mp3"
  );
  console.log(`Nota criada: ${audioResult.noteId}`);
  console.log(`Transcrição: ${audioResult.audioTranscript}`);
}
```

## Exemplo 8: Consulta Completa de Dados

```typescript
import AssistenteExecutivoClient from "./src/index";

const client = new AssistenteExecutivoClient();

async function consultaCompleta(contactId: string) {
  // Obter contato
  const contact = await client.contacts.getById(contactId);

  // Obter notas
  const notes = await client.notes.listByContact(contactId);

  // Obter lembretes
  const reminders = await client.reminders.list({ contactId });

  // Obter drafts relacionados
  const drafts = await client.automation.listDrafts({ contactId });

  return {
    contact,
    notes,
    reminders: reminders.items,
    drafts: drafts.items,
  };
}
```

## Exemplo 9: Script Completo para Cursor

```typescript
import AssistenteExecutivoClient from "./src/index";
import { ReminderStatus } from "./src/types";

const client = new AssistenteExecutivoClient({
  baseUrl: process.env.API_BASE_URL || "https://api.assistente.live",
});

async function scriptCompleto() {
  try {
    // Verificar autenticação
    const session = await client.auth.getSession();
    if (!session.authenticated) {
      console.log("❌ Não autenticado!");
      return;
    }

    console.log(`✅ Autenticado como: ${session.user?.email}`);

    // Listar contatos
    const contacts = await client.contacts.list();
    console.log(`\n📇 Total de contatos: ${contacts.totalCount}`);

    // Listar lembretes pendentes
    const reminders = await client.reminders.list({
      status: ReminderStatus.Pending,
    });
    console.log(`\n⏰ Lembretes pendentes: ${reminders.totalCount}`);

    // Verificar créditos
    const balance = await client.credits.getBalance();
    console.log(`\n💰 Saldo: ${balance.balance} créditos`);

  } catch (error: any) {
    console.error("❌ Erro:", error.message);
  }
}

scriptCompleto();
```

