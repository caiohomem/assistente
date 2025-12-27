/**
 * Exemplo básico de uso do cliente API
 */

import AssistenteExecutivoClient from "../src/index";

async function main() {
  // Criar instância do cliente
  const client = new AssistenteExecutivoClient({
    baseUrl: process.env.API_BASE_URL || "https://api.assistente.live",
    frontendUrl: process.env.FRONTEND_URL || "https://web.assistente.live",
  });

  try {
    // Verificar autenticação
    console.log("Verificando autenticação...");
    const session = await client.auth.getSession();
    
    if (!session.authenticated) {
      console.log("❌ Usuário não autenticado.");
      console.log(`🔗 Faça login em: ${client.auth.getLoginUrl()}`);
      return;
    }

    console.log("✅ Usuário autenticado:", session.user?.email);

    // Listar contatos
    console.log("\n📇 Listando contatos...");
    const contactsResult = await client.contacts.list({ page: 1, pageSize: 10 });
    console.log(`Total de contatos: ${contactsResult.totalCount}`);
    
    if (contactsResult.items.length > 0) {
      const firstContact = contactsResult.items[0];
      console.log(`Primeiro contato: ${firstContact.firstName} ${firstContact.lastName || ""}`);

      // Listar notas do contato
      console.log("\n📝 Listando notas do contato...");
      const notes = await client.notes.listByContact(firstContact.contactId);
      console.log(`Total de notas: ${notes.length}`);

      // Listar lembretes do contato
      console.log("\n⏰ Listando lembretes do contato...");
      const reminders = await client.reminders.list({
        contactId: firstContact.contactId,
        page: 1,
        pageSize: 10,
      });
      console.log(`Total de lembretes: ${reminders.totalCount}`);
    }

    // Obter saldo de créditos
    console.log("\n💰 Verificando saldo de créditos...");
    const balance = await client.credits.getBalance();
    console.log(`Saldo atual: ${balance.balance} créditos`);

    // Listar planos
    console.log("\n📦 Listando planos disponíveis...");
    const plans = await client.plans.list();
    console.log(`Total de planos: ${plans.length}`);
    plans.forEach((plan) => {
      console.log(`  - ${plan.name}: ${plan.price} (${plan.creditAmount} créditos)`);
    });

    console.log("\n✅ Exemplo concluído com sucesso!");
  } catch (error: any) {
    console.error("❌ Erro:", error.message);
    if (error.statusCode) {
      console.error(`   Status: ${error.statusCode}`);
    }
  }
}

// Executar
main().catch(console.error);

