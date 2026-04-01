# Descrição Funcional - Novos Módulos

## Visão Geral

Este documento descreve funcionalmente os novos módulos a serem implementados no Assistente Executivo, focando em:
- O que cada módulo faz
- Casos de uso principais
- Fluxos de trabalho
- Benefícios para o usuário

---

## Módulo 2: Intelligence (Inteligência)

### O que é

O módulo de **Intelligence** adiciona capacidades de inteligência artificial e análise de dados para enriquecer informações de contatos e identificar riscos. O sistema coleta dados de fontes públicas, analisa relacionamentos e gera relatórios de verificação.

### Funcionalidades Principais

#### 2.1 Enriquecimento de Perfis

**O que faz**: Coleta automaticamente informações públicas sobre contatos a partir de fontes externas (LinkedIn, redes sociais, sites corporativos) e consolida essas informações no perfil do contato.

**Como funciona**:
1. Usuário solicita enriquecimento de um contato
2. Sistema cria um job de processamento assíncrono
3. Providers externos (LinkedIn, APIs públicas) são consultados
4. Dados coletados são consolidados e validados
5. Snapshot do perfil enriquecido é criado
6. Contato é atualizado com novas informações (opcional, com aprovação do usuário)

**Informações coletadas**:
- Perfil profissional completo (LinkedIn)
- Histórico de cargos e empresas
- Educação e certificações
- Publicações e posts relevantes
- Conexões em comum
- Informações de empresas associadas

**Casos de Uso**:
- Executivo recebe cartão de visita → Digitaliza → Solicita enriquecimento → Perfil completo automaticamente
- Antes de reunião importante → Enriquecimento automático → Informações atualizadas → Preparação melhor
- Sistema detecta novo contato → Sugere enriquecimento → Usuário aprova → Dados coletados

**Benefícios**:
- Economia de tempo na pesquisa manual
- Informações sempre atualizadas
- Contexto completo antes de interações importantes

---

#### 2.2 Grafo de Relacionamentos

**O que faz**: Visualiza e analisa conexões entre contatos, identificando caminhos de relacionamento, conexões em comum e oportunidades de networking.

**Como funciona**:
1. Sistema analisa todos os relacionamentos cadastrados
2. Identifica conexões diretas e indiretas entre contatos
3. Calcula caminhos mais relevantes entre dois contatos
4. Identifica conexões em comum
5. Gera snapshot do grafo para visualização
6. Atualiza grafo periodicamente ou sob demanda

**Visualização**:
- Nós representam contatos
- Arestas representam relacionamentos
- Cores indicam tipos de relacionamento
- Tamanho dos nós indica importância/centralidade
- Caminhos destacados mostram conexões entre dois pontos

**Casos de Uso**:
- Executivo quer apresentar pessoa A para pessoa B → Sistema mostra caminho de conexão → Facilita introdução
- Usuário quer identificar conexões em comum com um contato importante → Grafo mostra → Oportunidade de networking
- Análise estratégica de rede → Visualização completa → Identifica hubs de relacionamento

**Benefícios**:
- Networking estratégico facilitado
- Identificação de oportunidades de conexão
- Compreensão visual da rede de relacionamentos

---

#### 2.3 KYC / Due Diligence

**O que faz**: Realiza verificação automatizada de riscos através de varredura em fontes públicas, identificando processos judiciais, menções negativas na mídia e outros "red flags".

**Como funciona**:
1. Usuário solicita verificação de risco para contato ou empresa
2. Sistema cria job de processamento
3. Providers de KYC varrem fontes públicas:
   - Tribunais (processos judiciais)
   - Receita Federal (situação cadastral)
   - Mídia (menções negativas)
   - Órgãos reguladores
   - Outras fontes públicas relevantes
4. Sistema consolida findings (achados)
5. Calcula score de risco (0-100)
6. Gera relatório estruturado com fontes
7. Relatório tem validade temporal (ex: 30 dias)

**Tipos de Verificação**:
- **Pessoa Física**: CPF, processos, situação cadastral
- **Pessoa Jurídica**: CNPJ, processos, situação cadastral, sócios
- **Verificação Completa**: Todas as fontes disponíveis

**Score de Risco**:
- **0-30**: Baixo risco (verde)
- **31-60**: Risco moderado (amarelo)
- **61-100**: Alto risco (vermelho)

**Casos de Uso**:
- Antes de fechar acordo importante → KYC solicitado → Relatório gerado → Decisão informada
- Parceiro novo → Verificação automática → Red flags identificados → Prevenção de problemas
- Auditoria periódica → KYC agendado → Relatórios atualizados → Compliance mantido

**Benefícios**:
- Redução de riscos em negócios
- Compliance facilitado
- Decisões mais informadas
- Prevenção de problemas futuros

---

## Módulo 3: Automation (Automação)

### O que é

O módulo de **Automation** automatiza tarefas repetitivas e estratégicas de relacionamento, incluindo lembretes inteligentes, geração automática de documentos e gestão de templates.

### Funcionalidades Principais

#### 3.1 Lembretes Inteligentes

**O que faz**: Sistema sugere automaticamente quando reativar ou nutrir relacionamentos com base em eventos, tempo sem contato e notícias relevantes.

**Como funciona**:
1. Sistema monitora continuamente:
   - Tempo desde último contato
   - Aniversários (pessoal e empresa)
   - Notícias relevantes sobre contato/empresa
   - Eventos importantes
   - Mudanças de status profissional
2. Quando detecta oportunidade, cria lembrete
3. Lembrete inclui:
   - Motivo do lembrete
   - Contexto relevante
   - Sugestão de mensagem
   - Timing sugerido
4. Usuário recebe notificação
5. Pode agir diretamente do lembrete (gerar draft, agendar, dispensar)

**Tipos de Lembretes**:
- **Tempo sem contato**: "Faz 3 meses sem contato com João"
- **Aniversário**: "Aniversário da empresa XYZ na próxima semana"
- **Notícia relevante**: "Empresa ABC foi mencionada positivamente na mídia"
- **Mudança profissional**: "Maria mudou de cargo - oportunidade de parabenizar"
- **Evento próximo**: "Reunião agendada com Pedro em 2 dias"

**Casos de Uso**:
- Sistema detecta 3 meses sem contato → Lembrete criado → Usuário visualiza → Gera email de follow-up
- Aniversário de empresa importante → Lembrete antecipado → Usuário prepara mensagem → Envia no momento certo
- Notícia positiva sobre cliente → Lembrete imediato → Oportunidade de parabenizar → Relacionamento fortalecido

**Benefícios**:
- Nunca perder oportunidades de contato
- Relacionamentos sempre nutridos
- Timing perfeito para interações
- Redução de trabalho manual

---

#### 3.2 Draft Documents (Rascunhos Automáticos)

**O que faz**: Gera automaticamente rascunhos de emails, ofícios e convites baseados em contexto do relacionamento, templates e inteligência artificial.

**Como funciona**:
1. Usuário solicita criação de draft (ou sistema sugere a partir de lembrete)
2. Sistema analisa contexto:
   - Histórico de interações com o contato
   - Tipo de relacionamento
   - Última interação
   - Objetivo da comunicação
3. Seleciona template apropriado
4. Preenche placeholders com dados do contato
5. IA ajusta tom e conteúdo conforme contexto
6. Aplica papel timbrado (se configurado)
7. Gera draft para revisão
8. Usuário revisa, edita se necessário e aprova/envia

**Tipos de Drafts**:
- **Email**: Comunicação informal ou formal
- **Ofício**: Documento formal
- **Convite**: Eventos, reuniões, parcerias

**Casos de Uso**:
- Lembrete de aniversário → Sistema gera draft de parabéns → Usuário revisa → Envia
- Follow-up após reunião → Draft gerado automaticamente → Personalizado com detalhes → Enviado
- Convite para evento → Template selecionado → Dados preenchidos → Papel timbrado aplicado → Revisão e envio

**Benefícios**:
- Economia de tempo na redação
- Consistência na comunicação
- Personalização automática
- Profissionalismo mantido

---

#### 3.3 Templates

**O que faz**: Modelos reutilizáveis de documentos com placeholders que podem ser preenchidos automaticamente com dados de contatos e contexto.

**Como funciona**:
1. Usuário cria template definindo:
   - Nome e descrição
   - Tipo de documento (email, ofício, convite)
   - Estrutura do texto
   - Placeholders (ex: {{nome}}, {{empresa}}, {{data}})
2. Template é salvo e fica disponível
3. Ao criar draft, template pode ser selecionado
4. Sistema preenche placeholders automaticamente
5. Conteúdo é personalizado para o contexto

**Placeholders Disponíveis**:
- Dados do contato: nome, empresa, cargo, email
- Dados da empresa: nome, domínio, setor
- Contexto: data, motivo, histórico
- Personalização: tom, formalidade

**Casos de Uso**:
- Executivo cria template de follow-up → Usa em múltiplos contatos → Consistência mantida
- Template de proposta comercial → Estrutura padronizada → Apenas dados específicos alterados
- Email de boas-vindas → Template criado → Automatizado para novos contatos

**Benefícios**:
- Padronização de comunicação
- Reutilização eficiente
- Manutenção centralizada
- Qualidade consistente

---

#### 3.4 Letterheads (Papéis Timbrados)

**O que faz**: Design personalizado de papel timbrado que pode ser aplicado a documentos, mantendo identidade visual profissional.

**Como funciona**:
1. Usuário cria papel timbrado definindo:
   - Nome/identificação
   - Design (logo, cores, layout)
   - Elementos visuais (cabeçalho, rodapé, bordas)
2. Design é salvo como template visual
3. Ao criar documento ou draft, papel timbrado pode ser aplicado
4. Sistema renderiza documento com design aplicado
5. Documento final mantém identidade visual

**Elementos do Design**:
- Logo/identidade visual
- Cabeçalho personalizado
- Rodapé com informações
- Cores e tipografia
- Layout profissional

**Casos de Uso**:
- Empresa cria papel timbrado corporativo → Aplicado a todos os documentos oficiais → Identidade mantida
- Documento importante → Papel timbrado aplicado → Profissionalismo reforçado
- Múltiplos papéis timbrados → Seleção conforme tipo de documento → Flexibilidade mantida

**Benefícios**:
- Identidade visual profissional
- Branding consistente
- Aparência polida
- Diferenciação

---

## Módulo 4: Documents (Documentos)

### O que é

O módulo de **Documents** fornece um sistema completo de criação, gerenciamento e versionamento de documentos formais, com suporte a templates e integração com papel timbrado.

### Funcionalidades Principais

#### 4.1 Criação e Edição de Documentos

**O que faz**: Editor completo de documentos com suporte a conteúdo rico, versionamento e controle de status.

**Como funciona**:
1. Usuário cria novo documento:
   - A partir de template (recomendado)
   - Do zero
   - Copiando documento existente
2. Editor rico permite:
   - Formatação de texto
   - Estruturação (títulos, parágrafos, listas)
   - Inserção de elementos
   - Aplicação de papel timbrado
3. Documento é salvo automaticamente
4. Versões são mantidas (histórico)
5. Status controla edição:
   - **Draft**: Em edição
   - **Review**: Em revisão
   - **Finalized**: Finalizado (não pode mais editar)

**Tipos de Documentos**:
- Contratos
- Ofícios
- Propostas
- Memorandos
- Outros documentos formais

**Casos de Uso**:
- Executivo precisa criar contrato → Seleciona template → Edita termos específicos → Finaliza
- Documento em negociação → Múltiplas versões criadas → Histórico mantido → Comparação facilitada
- Documento importante → Finalizado para evitar alterações → Versão preservada

**Benefícios**:
- Criação eficiente de documentos
- Controle de versões
- Prevenção de alterações acidentais
- Histórico completo

---

#### 4.2 Templates de Documentos

**O que faz**: Modelos reutilizáveis de documentos que podem ser instanciados e personalizados, garantindo consistência e economia de tempo.

**Como funciona**:
1. Usuário cria template definindo:
   - Nome e descrição
   - Tipo de documento
   - Estrutura completa
   - Placeholders para dados variáveis
2. Template é salvo e fica disponível
3. Ao criar documento, template pode ser selecionado
4. Sistema instancia documento a partir do template
5. Placeholders são preenchidos (manual ou automático)
6. Usuário personaliza conforme necessário

**Estrutura de Templates**:
- Cabeçalho padrão
- Seções estruturadas
- Cláusulas padrão
- Rodapé
- Placeholders estratégicos

**Casos de Uso**:
- Empresa padroniza contratos → Template criado → Todos os contratos seguem estrutura → Consistência legal
- Template de proposta → Estrutura profissional → Apenas valores e detalhes alterados → Eficiência
- Múltiplos templates → Seleção conforme necessidade → Flexibilidade mantida

**Benefícios**:
- Padronização
- Economia de tempo
- Consistência legal/formal
- Facilidade de manutenção

---

#### 4.3 Versionamento

**O que faz**: Controle completo de versões de documentos, permitindo rastreamento de alterações e recuperação de versões anteriores.

**Como funciona**:
1. Cada vez que documento é salvo, nova versão é criada
2. Versões são numeradas sequencialmente
3. Histórico mantém:
   - Versão número
   - Data/hora
   - Autor da alteração
   - Resumo das mudanças (opcional)
4. Usuário pode:
   - Visualizar versões anteriores
   - Comparar versões
   - Restaurar versão anterior
   - Ver diff (diferenças)

**Casos de Uso**:
- Negociação de contrato → Múltiplas versões → Comparação lado a lado → Decisão informada
- Alteração acidental → Versão anterior restaurada → Problema resolvido
- Auditoria → Histórico completo → Rastreabilidade mantida

**Benefícios**:
- Segurança (recuperação)
- Rastreabilidade
- Comparação facilitada
- Auditoria completa

---

## Módulo 5: Commission & Agreements (Comissões e Acordos)

### O que é

O módulo de **Commission & Agreements** gerencia acordos de comissão entre múltiplas partes, contas escrow para garantia de pagamentos e processos de negociação mediados.

### Funcionalidades Principais

#### 5.1 Acordos de Comissão

**O que faz**: Criação e gerenciamento de acordos entre múltiplas partes com divisão de valores, milestones e controle de status.

**Como funciona**:
1. Usuário cria acordo definindo:
   - Valor total do acordo
   - Partes envolvidas (contatos/empresas)
   - Percentual de cada parte
   - Milestones (marcos de pagamento)
   - Condições e termos
2. Partes são adicionadas ao acordo
3. Milestones são definidos com:
   - Descrição
   - Valor
   - Data de vencimento
   - Condições de cumprimento
4. Acordo pode ter conta escrow associada
5. Status controla o acordo:
   - **Draft**: Em elaboração
   - **PendingAcceptance**: Aguardando aceite das partes
   - **Active**: Ativo
   - **Completed**: Concluído
   - **Disputed**: Em disputa
   - **Canceled**: Cancelado
6. Aceite das partes:
   - Cada parte recebe uma proposta por email com link de aceite
   - Acordo permanece em PendingAcceptance até todas aceitarem
   - Lembretes são enviados diariamente enquanto houver pendências
   - Expira automaticamente após um número máximo de dias sem aceite

**Casos de Uso**:
- Parceria comercial → Acordo criado → Partes definidas → Milestones estabelecidos → Execução controlada
- Comissão de venda → Acordo criado → Percentuais definidos → Pagamentos automáticos quando milestones cumpridos
- Projeto com múltiplas fases → Milestones por fase → Controle de progresso → Pagamentos condicionais

**Benefícios**:
- Controle completo de acordos
- Transparência entre partes
- Execução organizada
- Redução de conflitos

---

#### 5.2 Contas Escrow

**O que faz**: Gestão de contas de garantia onde valores ficam retidos até cumprimento de condições, garantindo segurança para todas as partes.

**Como funciona**:
1. Conta escrow é criada associada a um acordo
2. Valores são depositados na conta escrow
3. Saldo é controlado e não pode ser movido sem aprovação
4. Quando milestone é cumprido:
   - Payout (pagamento) é solicitado
   - Partes relevantes são notificadas
   - Aprovações são coletadas
   - Valor é liberado para beneficiário
5. Em caso de disputa:
   - Valores ficam retidos
   - Processo de resolução é iniciado
   - Liberação depende de resolução

**Tipos de Payouts**:
- **Automatic**: Liberado automaticamente quando condições atendidas
- **Approval Required**: Requer aprovação de partes
- **Disputed**: Em disputa, requer resolução

**Casos de Uso**:
- Projeto com pagamento por etapas → Valores depositados em escrow → Milestone cumprido → Pagamento liberado → Segurança para ambas partes
- Acordo complexo → Escrow garante cumprimento → Reduz risco → Confiança aumentada
- Disputa surge → Valores retidos → Processo de resolução → Justiça para todas as partes

**Benefícios**:
- Segurança financeira
- Redução de riscos
- Confiança entre partes
- Resolução de disputas facilitada

---

#### 5.3 Milestones (Marcos)

**O que faz**: Controle de marcos de pagamento com valores e datas específicas, permitindo acompanhamento de progresso e liberação condicional de pagamentos.

**Como funciona**:
1. Milestones são criados no acordo definindo:
   - Descrição do marco
   - Valor associado
   - Data de vencimento
   - Condições de cumprimento
   - Status (Pending, Completed, Overdue)
2. Sistema monitora milestones:
   - Notifica quando próximos do vencimento
   - Alerta sobre atrasos
   - Verifica cumprimento de condições
3. Quando milestone é cumprido:
   - Status atualizado
   - Payout pode ser liberado (se escrow configurado)
   - Próximo milestone ativado

**Casos de Uso**:
- Projeto em fases → Milestone por fase → Progresso controlado → Pagamentos condicionais
- Venda com comissão → Milestone de fechamento → Pagamento automático → Motivação mantida
- Acordo complexo → Múltiplos milestones → Controle detalhado → Execução organizada

**Benefícios**:
- Controle de progresso
- Pagamentos condicionais
- Motivação para cumprimento
- Organização clara

---

#### 5.4 Mediador Inteligente (Negotiation Form)

**O que faz**: Formulário estruturado que facilita negociações complexas entre múltiplas partes, consolidando propostas e gerando acordos automaticamente.

**Como funciona**:
1. Formulário de negociação é criado para um acordo
2. Estrutura define:
   - Campos negociáveis (valores, prazos, condições)
   - Limites e restrições
   - Regras de validação
3. Partes preenchem suas propostas:
   - Valores desejados
   - Condições aceitáveis
   - Observações
4. Sistema consolida propostas:
   - Identifica pontos de convergência
   - Destaca divergências
   - Sugere termos intermediários
5. Negociação iterativa:
   - Partes ajustam propostas
   - Sistema recalcula
   - Convergência é alcançada
6. Quando acordo é alcançado:
   - Acordo é gerado automaticamente
   - Termos consolidados
   - Partes notificadas

**Casos de Uso**:
- Negociação complexa → Formulário estruturado → Propostas organizadas → Acordo facilitado
- Múltiplas partes → Consolidação automática → Visão clara → Decisão informada
- Termos técnicos → Validação automática → Erros prevenidos → Qualidade mantida

**Benefícios**:
- Negociação estruturada
- Consolidação automática
- Redução de tempo
- Acordos mais justos

---

## Módulo 6: Subscription (Assinaturas)

### O que é

O módulo de **Subscription** gerencia assinaturas de usuários a planos, controlando acesso a funcionalidades, limites do sistema e renovação automática.

### Funcionalidades Principais

#### 6.1 Gestão de Assinaturas

**O que faz**: Usuários podem assinar planos (mensal/anual) que definem limites e funcionalidades disponíveis no sistema.

**Como funciona**:
1. Usuário seleciona plano desejado:
   - **Básico**: Funcionalidades essenciais, limites menores
   - **Profissional**: Funcionalidades avançadas, limites maiores
   - **Enterprise**: Todas as funcionalidades, limites altos
2. Assinatura é criada:
   - Plano associado
   - Intervalo de cobrança (mensal/anual)
   - Data de início
   - Data de renovação
3. Limites são aplicados:
   - Número máximo de contatos
   - Armazenamento de mídia
   - Funcionalidades premium
   - Créditos mensais
4. Sistema valida limites antes de permitir operações
5. Renovação automática:
   - Sistema processa no vencimento
   - Período é estendido
   - Status mantido

**Limites por Plano**:
- **Básico**: 100 contatos, 1GB armazenamento, funcionalidades básicas
- **Profissional**: 1000 contatos, 10GB armazenamento, todas funcionalidades
- **Enterprise**: Ilimitado, todas funcionalidades, suporte prioritário

**Casos de Uso**:
- Novo usuário → Seleciona plano básico → Assinatura criada → Acesso liberado → Limites aplicados
- Usuário cresce → Upgrade para profissional → Limites aumentados → Funcionalidades premium liberadas
- Renovação automática → Sistema processa → Período estendido → Continuidade garantida

**Benefícios**:
- Modelo de negócio claro
- Escalabilidade controlada
- Acesso justo
- Renovação simplificada

---

#### 6.2 Controle de Acesso

**O que faz**: Sistema valida limites do plano antes de permitir operações, garantindo que usuários respeitem seus limites contratuais.

**Como funciona**:
1. Antes de operação crítica, sistema verifica:
   - Limite de contatos (ao criar novo contato)
   - Armazenamento disponível (ao fazer upload)
   - Funcionalidade premium (ao usar feature avançada)
   - Créditos disponíveis (ao usar IA)
2. Se limite não atingido:
   - Operação permitida
   - Limite atualizado
3. Se limite atingido:
   - Operação bloqueada
   - Mensagem informativa
   - Opção de upgrade sugerida

**Validações**:
- Criação de contato → Verifica limite máximo
- Upload de mídia → Verifica espaço disponível
- Uso de IA → Verifica créditos
- Funcionalidade premium → Verifica plano

**Casos de Uso**:
- Usuário tenta criar contato além do limite → Sistema bloqueia → Sugere upgrade → Decisão informada
- Upload de arquivo grande → Espaço insuficiente → Opções apresentadas → Solução encontrada
- Funcionalidade premium → Plano básico → Upgrade sugerido → Acesso após upgrade

**Benefícios**:
- Controle de recursos
- Prevenção de abusos
- Oportunidades de upgrade
- Transparência

---

#### 6.3 Cancelamento e Renovação

**O que faz**: Usuários podem cancelar assinaturas (mantendo acesso até fim do período) e sistema gerencia renovação automática.

**Como funciona**:

**Cancelamento**:
1. Usuário solicita cancelamento
2. Status da assinatura é atualizado
3. Acesso é mantido até fim do período pago
4. Renovação automática é desabilitada
5. Usuário pode reativar antes do vencimento

**Renovação**:
1. Sistema monitora datas de renovação
2. Antes do vencimento (ex: 7 dias), notifica usuário
3. No vencimento, processa renovação:
   - Cobrança processada (se automática)
   - Período estendido
   - Status mantido
4. Se falha na renovação:
   - Status atualizado (PastDue)
   - Acesso pode ser limitado
   - Notificações enviadas

**Casos de Uso**:
- Usuário cancela → Acesso mantido até fim do mês → Sem surpresas → Experiência positiva
- Renovação automática → Sistema processa → Transparência → Continuidade garantida
- Falha na cobrança → Status atualizado → Usuário notificado → Resolução facilitada

**Benefícios**:
- Flexibilidade para usuário
- Continuidade de serviço
- Transparência
- Gestão simplificada

---

## Resumo dos Benefícios por Módulo

### Intelligence
- **Enriquecimento**: Informações completas sem esforço manual
- **Grafo**: Networking estratégico facilitado
- **KYC**: Redução de riscos e compliance

### Automation
- **Lembretes**: Nunca perder oportunidades
- **Drafts**: Economia de tempo na comunicação
- **Templates**: Consistência e padronização
- **Letterheads**: Profissionalismo visual

### Documents
- **Criação**: Eficiência na elaboração
- **Templates**: Padronização e reutilização
- **Versionamento**: Segurança e rastreabilidade

### Commission
- **Acordos**: Controle completo e transparência
- **Escrow**: Segurança financeira
- **Milestones**: Organização e motivação
- **Mediador**: Negociação facilitada

### Subscription
- **Gestão**: Modelo de negócio claro
- **Controle**: Recursos gerenciados
- **Flexibilidade**: Cancelamento e renovação simples

---

## Integração entre Módulos

Os módulos trabalham de forma integrada:

- **Intelligence → Automation**: Dados enriquecidos alimentam lembretes inteligentes
- **Automation → Documents**: Drafts podem ser convertidos em documentos formais
- **Documents → Commission**: Documentos podem ser associados a acordos
- **Subscription → Todos**: Limites controlam acesso a todas as funcionalidades

Esta integração cria uma experiência completa e fluida para o usuário.

