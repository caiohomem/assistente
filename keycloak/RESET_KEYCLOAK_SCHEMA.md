# Reset do Schema do Keycloak

## Problema
Após limpar o banco de dados do Keycloak, você pode ver erros como:
```
ERROR: relation "offline_user_session" does not exist
```

## Solução

O Keycloak recria automaticamente o schema na inicialização. Você precisa **reiniciar o container do Keycloak**:

### Opção 1: Reiniciar via Docker Compose

```bash
cd keycloak
docker-compose -f docker-compose.production.yml restart keycloak
```

Ou se preferir recriar completamente:

```bash
cd keycloak
docker-compose -f docker-compose.production.yml down
docker-compose -f docker-compose.production.yml up -d
```

### Opção 2: Reiniciar apenas o container

```bash
docker restart keycloak
```

### Opção 3: Forçar rebuild do Keycloak

Se o problema persistir, force o Keycloak a recriar o schema:

```bash
cd keycloak
docker-compose -f docker-compose.production.yml down
# Aguarde alguns segundos
docker-compose -f docker-compose.production.yml up -d
```

## O que acontece

Quando o Keycloak inicia e detecta que as tabelas não existem, ele:
1. Executa as migrations do Liquibase
2. Cria todas as tabelas necessárias
3. Inicializa o schema do banco

## Verificar se funcionou

Após reiniciar, verifique os logs:

```bash
docker logs keycloak | grep -i "database\|schema\|migration"
```

Você deve ver mensagens como:
- "Database initialized"
- "Schema updated"
- "Migrations applied"

## Importante

⚠️ **NÃO** limpe o banco enquanto o Keycloak está rodando. Sempre:
1. Pare o Keycloak (`docker-compose down`)
2. Limpe o banco (se necessário)
3. Inicie o Keycloak novamente (`docker-compose up -d`)

O Keycloak recriará automaticamente o schema na inicialização.

