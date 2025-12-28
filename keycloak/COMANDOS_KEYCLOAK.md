# Comandos do Keycloak

## Parar o Keycloak

### Opção 1: Via Docker Compose (Recomendado)
```bash
cd keycloak
docker-compose -f docker-compose.production.yml down
```

### Opção 2: Parar apenas o container
```bash
docker stop keycloak
```

### Opção 3: Parar e remover o container
```bash
docker stop keycloak
docker rm keycloak
```

## Verificar se está rodando

```bash
docker ps | grep keycloak
```

Se não retornar nada, o Keycloak está parado.

## Iniciar o Keycloak

```bash
cd keycloak
docker-compose -f docker-compose.production.yml up -d
```

## Reiniciar o Keycloak

```bash
cd keycloak
docker-compose -f docker-compose.production.yml restart
```

Ou:

```bash
docker restart keycloak
```

## Ver logs do Keycloak

```bash
docker logs keycloak -f
```

## Processo Completo: Limpar Banco e Reiniciar

1. **Parar o Keycloak:**
   ```bash
   cd keycloak
   docker-compose -f docker-compose.production.yml down
   ```

2. **Limpar o banco** (execute o SQL no banco de dados)

3. **Iniciar o Keycloak novamente:**
   ```bash
   docker-compose -f docker-compose.production.yml up -d
   ```

4. **Aguardar inicialização e verificar logs:**
   ```bash
   docker logs keycloak -f
   ```

   Aguarde até ver mensagens como:
   - `Database initialized`
   - `Keycloak started`
   - `Schema updated`

5. **Verificar se está pronto:**
   ```bash
   docker logs keycloak | grep -i "started\|ready"
   ```

## Status do Container

```bash
docker ps -a | grep keycloak
```

Isso mostra se o container está rodando (STATUS: Up) ou parado (STATUS: Exited).

