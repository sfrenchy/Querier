import 'package:dio/dio.dart';
import 'api_endpoints.dart';

class ApiClient {
  final Dio _dio;
  final String baseUrl;

  ApiClient(this.baseUrl) : _dio = Dio();

  // Auth methods
  Future<Response> signIn(String email, String password) async {
    return _dio.post(
      ApiEndpoints.buildUrl(baseUrl, ApiEndpoints.signIn),
      data: {'email': email, 'password': password},
    );
  }

  Future<Response> signOut() async {
    return _dio.post(ApiEndpoints.buildUrl(baseUrl, ApiEndpoints.signOut));
  }

  Future<Response> refreshToken(String refreshToken) async {
    return _dio.post(
      ApiEndpoints.buildUrl(baseUrl, ApiEndpoints.refreshToken),
      data: {'refreshToken': refreshToken},
    );
  }

  // Settings methods
  Future<Response> getSettings() async {
    return _dio.get(ApiEndpoints.buildUrl(baseUrl, ApiEndpoints.getSettings));
  }

  Future<Response> updateSettings(Map<String, dynamic> settings) async {
    return _dio.post(
      ApiEndpoints.buildUrl(baseUrl, ApiEndpoints.updateSettings),
      data: settings,
    );
  }

  Future<bool> isConfigured() async {
    final response = await _dio.get(
      ApiEndpoints.buildUrl(baseUrl, ApiEndpoints.isConfigured),
    );
    return response.data as bool;
  }

  // Wizard methods
  Future<bool> setup({
    required String name,
    required String firstName,
    required String email,
    required String password,
    required String smtpHost,
    required int smtpPort,
    required String smtpUsername,
    required String smtpPassword,
    required bool useSSL,
  }) async {
    final response = await _dio.post(
      ApiEndpoints.buildUrl(baseUrl, ApiEndpoints.setup),
      data: {
        'admin': {
          'name': name,
          'firstName': firstName,
          'email': email,
          'password': password,
        },
        'smtp': {
          'host': smtpHost,
          'port': smtpPort,
          'username': smtpUsername,
          'password': smtpPassword,
          'useSSL': useSSL,
        },
      },
    );
    return response.statusCode == 200;
  }

  // User methods
  Future<Response> getCurrentUser() async {
    return _dio.get(ApiEndpoints.buildUrl(baseUrl, ApiEndpoints.currentUser));
  }

  Future<Response> getUserById(String id) async {
    final endpoint = ApiEndpoints.replaceUrlParams(
      ApiEndpoints.userById,
      {'id': id},
    );
    return _dio.get(ApiEndpoints.buildUrl(baseUrl, endpoint));
  }

  // Helper method pour configurer les headers d'authentification
  void setAuthToken(String token) {
    _dio.options.headers['Authorization'] = 'Bearer $token';
  }

  void clearAuthToken() {
    _dio.options.headers.remove('Authorization');
  }
}
