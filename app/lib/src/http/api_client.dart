import 'dart:convert';

import 'package:http/http.dart' as http;

class ApiClient {
  ApiClient({required String baseUrl}) : _baseUrl = _trimTrailingSlash(baseUrl);

  final String _baseUrl;

  Future<Map<String, dynamic>> getJson({
    required String path,
    String? bearerToken,
  }) async {
    final uri = Uri.parse('$_baseUrl$path');
    final headers = <String, String>{
      'Accept': 'application/json',
    };

    if (bearerToken != null && bearerToken.isNotEmpty) {
      headers['Authorization'] = 'Bearer $bearerToken';
    }

    final res = await http.get(uri, headers: headers);
    final body = res.body.isEmpty ? '{}' : res.body;

    if (res.statusCode < 200 || res.statusCode >= 300) {
      throw HttpException(
        statusCode: res.statusCode,
        body: body,
      );
    }

    final decoded = jsonDecode(body);
    if (decoded is Map<String, dynamic>) return decoded;
    return <String, dynamic>{'data': decoded};
  }
}

class HttpException implements Exception {
  HttpException({required this.statusCode, required this.body});

  final int statusCode;
  final String body;

  @override
  String toString() => 'HTTP $statusCode: $body';
}

String _trimTrailingSlash(String value) =>
    value.endsWith('/') ? value.substring(0, value.length - 1) : value;


