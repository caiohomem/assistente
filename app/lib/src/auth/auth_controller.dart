import 'dart:convert';

import 'package:flutter/foundation.dart';

import '../config/app_config.dart';
import '../http/api_client.dart';
import 'auth_service.dart';
import 'auth_storage.dart';
import 'auth_tokens.dart';

class AuthController extends ChangeNotifier {
  AuthController({
    AuthService? authService,
    AuthStorage? storage,
    ApiClient? api,
  })  : _authService = authService ?? AuthService(),
        _storage = storage ?? AuthStorage(),
        _api = api ?? ApiClient(baseUrl: AppConfig.apiBaseUrl) {
    _bootstrap();
  }

  final AuthService _authService;
  final AuthStorage _storage;
  final ApiClient _api;

  AuthTokens? _tokens;
  String? _lastApiResponse;
  String? _error;
  bool _busy = false;

  AuthTokens? get tokens => _tokens;
  String? get lastApiResponse => _lastApiResponse;
  String? get error => _error;
  bool get busy => _busy;

  Future<void> _bootstrap() async {
    try {
      _tokens = await _storage.read();
    } catch (e) {
      _error = 'Falha ao ler storage: $e';
    } finally {
      notifyListeners();
    }
  }

  Future<void> login() async {
    await _runBusy(() async {
      _error = null;
      
      // Try up to 2 times if state is lost (common with long OAuth flows like Google)
      int attempts = 0;
      const maxAttempts = 2;
      
      while (attempts < maxAttempts) {
        try {
          final result = await _authService.loginPkce();
          _tokens = result;
          await _storage.write(result);
          return; // Success
        } catch (e) {
          final errorMsg = e.toString();
          // If "No stored state" and we haven't exhausted attempts, retry once
          if ((errorMsg.contains('No stored state') || 
               errorMsg.contains('unable to handle response')) &&
              attempts < maxAttempts - 1) {
            attempts++;
            // Wait a bit before retry to let the system settle
            await Future.delayed(const Duration(milliseconds: 500));
            continue;
          }
          // Otherwise, throw the error
          rethrow;
        }
      }
    });
  }

  Future<void> logout() async {
    await _runBusy(() async {
      _error = null;
      _tokens = null;
      _lastApiResponse = null;
      await _storage.clear();
    });
  }

  Future<void> refresh() async {
    await _runBusy(() async {
      _error = null;
      final current = _tokens;
      if (current == null) throw StateError('Não autenticado.');

      final refreshed = await _authService.refresh(current);
      _tokens = refreshed;
      await _storage.write(refreshed);
    });
  }

  Future<void> callProtectedMe() async {
    await _runBusy(() async {
      _error = null;
      _lastApiResponse = null;

      var current = _tokens;
      if (current == null) throw StateError('Não autenticado.');

      // If token is expiring (or expired), try refresh first.
      final exp = current.expiresAt;
      if (exp != null && exp.isBefore(DateTime.now().add(const Duration(seconds: 30)))) {
        if (current.hasRefreshToken) {
          current = await _authService.refresh(current);
          _tokens = current;
          await _storage.write(current);
        }
      }

      final response = await _api.getJson(
        path: '/api/me',
        bearerToken: current.accessToken,
      );

      _lastApiResponse = const JsonEncoder.withIndent('  ').convert(response);
    });
  }

  Future<void> _runBusy(Future<void> Function() fn) async {
    _busy = true;
    notifyListeners();
    try {
      await fn();
    } catch (e) {
      _error = e.toString();
    } finally {
      _busy = false;
      notifyListeners();
    }
  }
}


