import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../auth/auth_controller.dart';
import '../config/app_config.dart';

class HomePage extends StatelessWidget {
  const HomePage({super.key});

  @override
  Widget build(BuildContext context) {
    final auth = context.watch<AuthController>();

    return Scaffold(
      appBar: AppBar(
        title: const Text('Assistente Executivo (Mobile)'),
      ),
      body: SafeArea(
        child: ListView(
          padding: const EdgeInsets.all(16),
          children: [
            _ConfigCard(),
            const SizedBox(height: 12),
            _StatusCard(),
            const SizedBox(height: 12),
            _ActionsCard(),
            const SizedBox(height: 12),
            if (auth.error != null) _ErrorCard(message: auth.error!),
            if (auth.lastApiResponse != null) _ResponseCard(text: auth.lastApiResponse!),
          ],
        ),
      ),
    );
  }
}

class _ConfigCard extends StatelessWidget {
  @override
  Widget build(BuildContext context) {
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(12),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const Text('Config', style: TextStyle(fontWeight: FontWeight.w600)),
            const SizedBox(height: 8),
            Text('API: ${AppConfig.apiBaseUrl}'),
            Text('Issuer: ${AppConfig.issuer}'),
            Text('ClientId: ${AppConfig.keycloakClientId}'),
            Text('RedirectUri: ${AppConfig.redirectUri}'),
          ],
        ),
      ),
    );
  }
}

class _StatusCard extends StatelessWidget {
  @override
  Widget build(BuildContext context) {
    final auth = context.watch<AuthController>();
    final t = auth.tokens;

    return Card(
      child: Padding(
        padding: const EdgeInsets.all(12),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const Text('Sessão', style: TextStyle(fontWeight: FontWeight.w600)),
            const SizedBox(height: 8),
            Text('Autenticado: ${t != null}'),
            if (t != null) ...[
              Text('Expira em: ${t.expiresAtIso ?? "(desconhecido)"}'),
              Text('Refresh token: ${t.hasRefreshToken ? "sim" : "não"}'),
              const SizedBox(height: 8),
              const Text('Access token (prefixo):', style: TextStyle(fontWeight: FontWeight.w600)),
              Text(
                t.accessToken.length > 24 ? '${t.accessToken.substring(0, 24)}…' : t.accessToken,
                style: const TextStyle(fontFamily: 'monospace'),
              ),
            ],
          ],
        ),
      ),
    );
  }
}

class _ActionsCard extends StatelessWidget {
  @override
  Widget build(BuildContext context) {
    final auth = context.watch<AuthController>();

    return Card(
      child: Padding(
        padding: const EdgeInsets.all(12),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const Text('Ações (Keycloak PKCE + Bearer)', style: TextStyle(fontWeight: FontWeight.w600)),
            const SizedBox(height: 12),
            Wrap(
              spacing: 8,
              runSpacing: 8,
              children: [
                FilledButton(
                  onPressed: auth.busy ? null : () => auth.login(),
                  child: auth.busy ? const Text('...') : const Text('Login (PKCE)'),
                ),
                OutlinedButton(
                  onPressed: auth.busy ? null : () => auth.refresh(),
                  child: const Text('Refresh'),
                ),
                OutlinedButton(
                  onPressed: auth.busy ? null : () => auth.callProtectedMe(),
                  child: const Text('Call /api/me'),
                ),
                TextButton(
                  onPressed: auth.busy ? null : () => auth.logout(),
                  child: const Text('Logout'),
                ),
              ],
            ),
          ],
        ),
      ),
    );
  }
}

class _ErrorCard extends StatelessWidget {
  const _ErrorCard({required this.message});
  final String message;

  @override
  Widget build(BuildContext context) {
    return Card(
      color: Theme.of(context).colorScheme.errorContainer,
      child: Padding(
        padding: const EdgeInsets.all(12),
        child: Text(message),
      ),
    );
  }
}

class _ResponseCard extends StatelessWidget {
  const _ResponseCard({required this.text});
  final String text;

  @override
  Widget build(BuildContext context) {
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(12),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const Text('Resposta', style: TextStyle(fontWeight: FontWeight.w600)),
            const SizedBox(height: 8),
            SelectableText(
              text,
              style: const TextStyle(fontFamily: 'monospace'),
            ),
          ],
        ),
      ),
    );
  }
}
