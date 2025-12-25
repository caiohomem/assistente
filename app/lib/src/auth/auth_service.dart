import 'package:flutter_appauth/flutter_appauth.dart';

import '../config/app_config.dart';
import 'auth_tokens.dart';

class AuthService {
  AuthService({FlutterAppAuth? appAuth}) : _appAuth = appAuth ?? const FlutterAppAuth();

  final FlutterAppAuth _appAuth;

  Future<AuthTokens> loginPkce() async {
    final request = AuthorizationTokenRequest(
      AppConfig.keycloakClientId,
      AppConfig.redirectUri,
      issuer: AppConfig.issuer,
      scopes: AppConfig.scopes,
      allowInsecureConnections: true,
      promptValues: const ['login'],
      // Explicit service configuration to avoid discovery issues
      serviceConfiguration: AuthorizationServiceConfiguration(
        authorizationEndpoint: '${AppConfig.issuer}/protocol/openid-connect/auth',
        tokenEndpoint: '${AppConfig.issuer}/protocol/openid-connect/token',
        endSessionEndpoint: '${AppConfig.issuer}/protocol/openid-connect/logout',
      ),
    );

    try {
      final result = await _appAuth.authorizeAndExchangeCode(request);
      if (result == null || result.accessToken == null || result.accessToken!.isEmpty) {
        throw StateError('Login cancelado ou token não retornado.');
      }

      return AuthTokens(
        accessToken: result.accessToken!,
        idToken: result.idToken,
        refreshToken: result.refreshToken,
        expiresAtIso: result.accessTokenExpirationDateTime?.toIso8601String(),
      );
    } catch (e) {
      // "No stored state" usually means the app process was recreated during login
      // This is common with long OAuth flows (e.g., Google → Keycloak → App)
      // The AuthController will retry automatically, but we provide a clear error message
      final errorMsg = e.toString();
      if (errorMsg.contains('No stored state') || 
          errorMsg.contains('unable to handle response')) {
        throw StateError(
          'State perdido durante login (tentativa ${DateTime.now().millisecondsSinceEpoch % 1000}). '
          'Isso pode acontecer em fluxos longos (ex.: Google OAuth). '
          'O app tentará novamente automaticamente. Se persistir, tente fazer login direto no Keycloak (sem Google).'
        );
      }
      rethrow;
    }
  }

  Future<AuthTokens> refresh(AuthTokens current) async {
    if (!current.hasRefreshToken) {
      throw StateError('Sem refresh_token para renovar a sessão.');
    }

    final request = TokenRequest(
      AppConfig.keycloakClientId,
      AppConfig.redirectUri,
      issuer: AppConfig.issuer,
      refreshToken: current.refreshToken,
      scopes: AppConfig.scopes,
      allowInsecureConnections: true,
    );

    final result = await _appAuth.token(request);
    if (result == null || result.accessToken == null || result.accessToken!.isEmpty) {
      throw StateError('Refresh falhou: token não retornado.');
    }

    return AuthTokens(
      accessToken: result.accessToken!,
      idToken: result.idToken ?? current.idToken,
      refreshToken: result.refreshToken ?? current.refreshToken,
      expiresAtIso: result.accessTokenExpirationDateTime?.toIso8601String() ??
          current.expiresAtIso,
    );
  }
}


