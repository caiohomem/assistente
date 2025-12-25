import 'dart:io' show Platform;

class AppConfig {
  AppConfig._();

  static const String keycloakRealm =
      String.fromEnvironment('KEYCLOAK_REALM', defaultValue: 'assistenteexecutivo');

  static const String keycloakClientId =
      String.fromEnvironment('KEYCLOAK_CLIENT_ID', defaultValue: 'assistenteexecutivo-app');

  static String get apiBaseUrl {
    const fromDefine = String.fromEnvironment('API_BASE_URL');
    if (fromDefine.isNotEmpty) return _trimTrailingSlash(fromDefine);

    // For Android emulator: 10.0.2.2 is the host machine
    // The API runs locally, so we still use the emulator tunnel
    final host = _defaultLocalHost();
    return 'http://$host:5239';
  }

  static String get keycloakBaseUrl {
    const fromDefine = String.fromEnvironment('KEYCLOAK_BASE_URL');
    if (fromDefine.isNotEmpty) return _trimTrailingSlash(fromDefine);

    // Default: use public Keycloak URL for consistent issuer matching with API
    // The API validates JWT issuer against PublicBaseUrl, so mobile must use the same
    return 'https://auth.callback-local-cchagas.xyz';
  }

  static String get issuer => '$keycloakBaseUrl/realms/$keycloakRealm';

  /// Must be registered as redirect URI in Keycloak client.
  /// Also must be configured on Android/iOS as a deep link.
  /// Format: scheme://host/path - using scheme-only redirect for AppAuth compatibility
  static const String redirectUri = 'com.assistenteexecutivo.app://oauth/callback';

  static const List<String> scopes = <String>['openid', 'profile', 'email'];

  static String _defaultLocalHost() {
    // Android emulator: host machine is 10.0.2.2
    if (Platform.isAndroid) return '10.0.2.2';
    // iOS simulator / desktop: localhost is OK
    return 'localhost';
  }

  static String _trimTrailingSlash(String value) =>
      value.endsWith('/') ? value.substring(0, value.length - 1) : value;
}


