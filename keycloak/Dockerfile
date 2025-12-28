# syntax=docker/dockerfile:1

# ---- build stage (Keycloak Quarkus build) ----
FROM quay.io/keycloak/keycloak:26.0.0 as builder

# Copia themes para dentro da imagem
COPY themes/ /opt/keycloak/themes/

# Compila a distribuição do Keycloak com o conteúdo adicionado
RUN /opt/keycloak/bin/kc.sh build

# ---- runtime stage ----
FROM quay.io/keycloak/keycloak:26.0.0

# Copia a distribuição compilada do builder
COPY --from=builder /opt/keycloak/ /opt/keycloak/

# Keycloak image typically runs as UID 1000 (keycloak)
USER 1000

ENTRYPOINT ["/opt/keycloak/bin/kc.sh"]

