import 'dart:convert';
import 'package:dio/dio.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'api_endpoints.dart';
import 'package:querier/models/user.dart';
import 'package:querier/models/role.dart';

class ApiClient {
  final Dio _dio;
  String baseUrl;
  final FlutterSecureStorage _secureStorage = const FlutterSecureStorage();

  ApiClient(this.baseUrl) : _dio = Dio() {
    updateBaseUrl(baseUrl);
  }

  void updateBaseUrl(String newBaseUrl) {
    baseUrl = newBaseUrl;
    _dio.options.baseUrl = baseUrl;
  }

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

  Future<Response> getUserData(String id) async {
    final endpoint = ApiEndpoints.replaceUrlParams(
      ApiEndpoints.userProfile,
      {'id': id},
    );
    return _dio.get(ApiEndpoints.buildUrl(baseUrl, endpoint));
  }

  Future<List<String>> getRecentQueries() async {
    final response = await _dio.get(
      ApiEndpoints.buildUrl(baseUrl, ApiEndpoints.recentQueries),
    );
    return List<String>.from(response.data);
  }

  Future<Map<String, int>> getQueryStats() async {
    final response = await _dio.get(
      ApiEndpoints.buildUrl(baseUrl, ApiEndpoints.queryStats),
    );
    return Map<String, int>.from(response.data);
  }

  Future<List<Map<String, dynamic>>> getActivityData() async {
    final response = await _dio.get(
      ApiEndpoints.buildUrl(baseUrl, ApiEndpoints.activity),
    );
    return List<Map<String, dynamic>>.from(response.data);
  }

  Future<void> logout() async {
    await _secureStorage.delete(key: 'access_token');
    await _secureStorage.delete(key: 'refresh_token');
    await _dio.post(
      ApiEndpoints.buildUrl(baseUrl, ApiEndpoints.signOut),
      data: {},
    );
  }

  Future<List<User>> getAllUsers() async {
    try {
      final response = await _dio.get(
        ApiEndpoints.buildUrl(baseUrl, ApiEndpoints.users),
      );

      print('API Response: ${response.data}'); // Debug log

      if (response.data is List) {
        return (response.data as List).map((userData) {
          print('Processing user data: $userData'); // Debug log
          return User.fromJson(userData);
        }).toList();
      } else {
        // Si la réponse contient une propriété data ou users
        final usersList = response.data['data'] ?? response.data['users'] ?? [];
        return (usersList as List)
            .map((userData) => User.fromJson(userData))
            .toList();
      }
    } catch (e, stackTrace) {
      print('Error in getAllUsers: $e\n$stackTrace'); // Debug log
      rethrow;
    }
  }

  Future<List<Role>> getAllRoles() async {
    try {
      final response = await _dio.get(
        ApiEndpoints.buildUrl(baseUrl, ApiEndpoints.roles),
      );

      if (response.data is List) {
        return (response.data as List)
            .map((roleData) => Role.fromJson(roleData))
            .toList();
      } else {
        final rolesList = response.data['data'] ?? response.data['roles'] ?? [];
        return (rolesList as List)
            .map((roleData) => Role.fromJson(roleData))
            .toList();
      }
    } catch (e) {
      print('Error in getAllRoles: $e');
      rethrow;
    }
  }
}
