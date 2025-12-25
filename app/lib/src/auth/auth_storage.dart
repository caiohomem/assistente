import 'package:flutter_secure_storage/flutter_secure_storage.dart';

import 'auth_tokens.dart';

class AuthStorage {
  AuthStorage({FlutterSecureStorage? storage})
      : _storage = storage ??
            const FlutterSecureStorage(
              aOptions: AndroidOptions(encryptedSharedPreferences: true),
            );

  final FlutterSecureStorage _storage;

  static const _kAccessToken = 'ae.access_token';
  static const _kIdToken = 'ae.id_token';
  static const _kRefreshToken = 'ae.refresh_token';
  static const _kExpiresAtIso = 'ae.expires_at_iso';

  Future<AuthTokens?> read() async {
    final accessToken = await _storage.read(key: _kAccessToken);
    if (accessToken == null || accessToken.isEmpty) return null;

    final idToken = await _storage.read(key: _kIdToken);
    final refreshToken = await _storage.read(key: _kRefreshToken);
    final expiresAtIso = await _storage.read(key: _kExpiresAtIso);

    return AuthTokens(
      accessToken: accessToken,
      idToken: (idToken?.isEmpty ?? true) ? null : idToken,
      refreshToken: (refreshToken?.isEmpty ?? true) ? null : refreshToken,
      expiresAtIso: (expiresAtIso?.isEmpty ?? true) ? null : expiresAtIso,
    );
  }

  Future<void> write(AuthTokens tokens) async {
    await _storage.write(key: _kAccessToken, value: tokens.accessToken);
    await _storage.write(key: _kIdToken, value: tokens.idToken ?? '');
    await _storage.write(key: _kRefreshToken, value: tokens.refreshToken ?? '');
    await _storage.write(key: _kExpiresAtIso, value: tokens.expiresAtIso ?? '');
  }

  Future<void> clear() async {
    await _storage.delete(key: _kAccessToken);
    await _storage.delete(key: _kIdToken);
    await _storage.delete(key: _kRefreshToken);
    await _storage.delete(key: _kExpiresAtIso);
  }
}


