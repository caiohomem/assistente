# Como Resolver o Problema do Admin no Keycloak

O Keycloak está mostrando a mensagem "Local access required" e pedindo para criar o usuário administrativo.

## Solução Rápida

Como o Keycloak está pedindo acesso local e já configuramos `KC_BOOTSTRAP_ADMIN_USERNAME` e `KC_BOOTSTRAP_ADMIN_PASSWORD`, a solução mais simples é:

1. **Acessar o Keycloak via localhost no servidor** usando um túnel SSH:

```bash
# No seu computador local, crie um túnel SSH:
ssh -L 8080:localhost:8080 -i ~/Downloads/lightsail-key.pem ubuntu@99.80.217.123

# Em outro terminal, acesse:
# https://localhost:8080
```

2. **Ou criar o admin diretamente no banco** usando SQL (mais complexo, requer hash válido).

## Solução Alternativa: Criar Admin via SQL

Se você quiser criar o admin diretamente no banco, precisa de um hash bcrypt válido para a senha "admin". O Keycloak usa pbkdf2-sha256 com 27500 iterações.

**Nota**: A criação direta no banco é complexa e pode causar inconsistências. É melhor usar o método de acesso local ou aguardar o Keycloak criar automaticamente.

## Verificar se Admin Foi Criado

```bash
# No servidor Lightsail:
psql 'postgresql://neondb_owner:npg_dcn6oJIReT0Z@ep-spring-star-abbjctg7-pooler.eu-west-2.aws.neon.tech/neondb?sslmode=require&channel_binding=require' -c "SELECT id, username, enabled FROM user_entity WHERE username = 'admin';"
```

## Próximos Passos

1. Tente acessar via túnel SSH primeiro
2. Se não funcionar, podemos criar o admin via SQL com um hash válido

