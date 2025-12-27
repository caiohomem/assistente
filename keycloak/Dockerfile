# syntax=docker/dockerfile:1

FROM quay.io/keycloak/keycloak:latest

USER root

# Embed the custom theme in the image so it survives container re-creation.
COPY ./themes/assistenteexecutivo /opt/keycloak/themes/assistenteexecutivo

# Keycloak image typically runs as UID 1000 (keycloak). Keep permissions compatible.
RUN chown -R 1000:0 /opt/keycloak/themes/assistenteexecutivo && chmod -R g+rwX /opt/keycloak/themes/assistenteexecutivo

USER 1000

