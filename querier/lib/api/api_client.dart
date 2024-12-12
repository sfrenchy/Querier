import 'dart:convert';
import 'package:dio/dio.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'api_endpoints.dart';
import 'package:querier/models/user.dart';
import 'package:querier/models/role.dart';
import 'package:querier/models/api_configuration.dart';
import 'package:flutter/material.dart';
import 'package:querier/models/db_connection.dart';
import 'package:querier/models/menu_category.dart';
import 'package:querier/models/page.dart';

class ApiClient {
  final Dio _dio;
  String baseUrl;
  final FlutterSecureStorage _secureStorage;
  final NavigatorState _navigator;

  ApiClient(this.baseUrl, this._navigator)
      : _dio = Dio(),
        _secureStorage = const FlutterSecureStorage() {
    updateBaseUrl(baseUrl);
    _setupInterceptors();
  }

  void _setupInterceptors() {
    _dio.interceptors.add(
      InterceptorsWrapper(
        onError: (error, handler) async {
          if (error.response?.statusCode == 401) {
            await _secureStorage.delete(key: 'access_token');
            await _secureStorage.delete(key: 'refresh_token');

            _navigator.pushNamedAndRemoveUntil('/login', (route) => false);
          }
          return handler.next(error);
        },
      ),
    );
  }

  void updateBaseUrl(String newBaseUrl) {
    baseUrl = newBaseUrl;
    _dio.options.baseUrl = baseUrl;
  }

  // Auth methods
  Future<Response> signIn(String email, String password) async {
    print('Attempting sign in for email: $email');
    final response = await _dio.post(
      ApiEndpoints.buildUrl(baseUrl, ApiEndpoints.signIn),
      data: {'email': email, 'password': password},
    );
    print('Sign in response: ${response.data}');
    return response;
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
    required String senderEmail,
    required String senderName,
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
          'senderEmail': senderEmail,
          'senderName': senderName,
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

  Future<bool> addRole(String name) async {
    try {
      final response = await _dio.post(
        ApiEndpoints.buildUrl(baseUrl, ApiEndpoints.addRole),
        data: {'id': '', 'name': name},
      );
      return response.statusCode == 200;
    } catch (e) {
      print('Error in addRole: $e');
      rethrow;
    }
  }

  Future<bool> updateRole(String id, String name) async {
    try {
      final response = await _dio.post(
        ApiEndpoints.buildUrl(baseUrl, ApiEndpoints.updateRole),
        data: {
          'Id': id,
          'Name': name,
        },
      );
      return response.statusCode == 200;
    } catch (e) {
      print('Error in updateRole: $e');
      rethrow;
    }
  }

  Future<bool> deleteRole(String id) async {
    try {
      final response = await _dio.delete(
        ApiEndpoints.buildUrl(
          baseUrl,
          ApiEndpoints.replaceUrlParams(ApiEndpoints.deleteRole, {'id': id}),
        ),
      );
      return response.statusCode == 200;
    } catch (e) {
      print('Error in deleteRole: $e');
      rethrow;
    }
  }

  Future<void> storeRefreshToken(String refreshToken) async {
    await _secureStorage.write(key: 'refresh_token', value: refreshToken);
  }

  Future<bool> addUser(String email, String firstName, String lastName,
      String password, List<String> roles) async {
    try {
      final response = await _dio.put(
        ApiEndpoints.buildUrl(baseUrl, ApiEndpoints.addUser),
        data: {
          'email': email,
          'firstName': firstName,
          'lastName': lastName,
          'password': password,
          'userName': email,
          'roles': roles,
        },
      );
      return response.statusCode == 200;
    } catch (e) {
      print('Error in addUser: $e');
      rethrow;
    }
  }

  Future<bool> updateUser(String id, String email, String firstName,
      String lastName, List<String> roles) async {
    try {
      final response = await _dio.put(
        ApiEndpoints.buildUrl(baseUrl, ApiEndpoints.updateUser),
        data: {
          'id': id,
          'email': email,
          'firstName': firstName,
          'lastName': lastName,
          'userName': email,
          'roles': roles,
        },
      );
      return response.statusCode == 200;
    } catch (e) {
      print('Error in updateUser: $e');
      rethrow;
    }
  }

  Future<bool> deleteUser(String id) async {
    try {
      final response = await _dio.delete(
        ApiEndpoints.buildUrl(
          baseUrl,
          ApiEndpoints.replaceUrlParams(ApiEndpoints.deleteUser, {'id': id}),
        ),
      );
      return response.statusCode == 200;
    } catch (e) {
      print('Error in deleteUser: $e');
      rethrow;
    }
  }

  Future<bool> resendConfirmationEmail(String userId) async {
    try {
      final response = await _dio.post(
        ApiEndpoints.buildUrl(baseUrl, ApiEndpoints.resendConfirmationEmail),
        data: {'userId': userId},
      );
      return response.statusCode == 200;
    } catch (e) {
      print('Error resending confirmation email: $e');
      rethrow;
    }
  }

  Future<ApiConfiguration> getApiConfiguration() async {
    try {
      final response = await _dio.get(
        ApiEndpoints.buildUrl(baseUrl, ApiEndpoints.apiConfiguration),
      );
      return ApiConfiguration.fromJson(response.data);
    } catch (e) {
      print('Error in getApiConfiguration: $e');
      rethrow;
    }
  }

  Future<bool> updateApiConfiguration(ApiConfiguration config) async {
    try {
      final response = await _dio.post(
        ApiEndpoints.buildUrl(baseUrl, ApiEndpoints.apiConfiguration),
        data: config.toJson(),
      );
      return response.statusCode == 200;
    } catch (e) {
      return false;
    }
  }

  Future<Response> put(String endpoint, {dynamic data}) async {
    final response = await _dio.put(
      ApiEndpoints.buildUrl(baseUrl, endpoint),
      data: data,
    );
    return response;
  }

  Future<Response> post(String endpoint, {dynamic data}) async {
    final response = await _dio.post(
      ApiEndpoints.buildUrl(baseUrl, endpoint),
      data: data,
    );
    return response;
  }

  Future<Response> get(String endpoint) async {
    final response = await _dio.get(
      ApiEndpoints.buildUrl(baseUrl, endpoint),
    );
    return response;
  }

  Future<Response> delete(String endpoint, {dynamic data}) async {
    final response = await _dio.delete(
      ApiEndpoints.buildUrl(baseUrl, endpoint),
      data: data,
    );
    return response;
  }

  Future<List<DBConnection>> getDBConnections() async {
    final response = await _dio.get(
      ApiEndpoints.buildUrl(baseUrl, ApiEndpoints.dbConnections),
    );
    return (response.data as List)
        .map((json) => DBConnection.fromJson(json))
        .toList();
  }

  Future<List<MenuCategory>> getMenuCategories() async {
    final response = await get(ApiEndpoints.menuCategories);
    return (response.data as List<dynamic>)
        .map((json) => MenuCategory.fromJson(json as Map<String, dynamic>))
        .toList();
  }

  Future<MenuCategory> createMenuCategory(Map<String, dynamic> data) async {
    final response = await post(ApiEndpoints.menuCategories, data: data);
    return MenuCategory.fromJson(response.data);
  }

  Future<MenuCategory> updateMenuCategory(int id, MenuCategory category) async {
    final response = await put(
      '${ApiEndpoints.menuCategories}/$id',
      data: category.toJson(),
    );
    return MenuCategory.fromJson(response.data);
  }

  Future<void> deleteMenuCategory(int id) async {
    await delete('${ApiEndpoints.menuCategories}/$id');
  }

  // Récupérer toutes les pages d'une catégorie
  Future<List<MenuPage>> getPages(int categoryId) async {
    final response = await _dio.get('/page?categoryId=$categoryId');
    List<MenuPage> pages = List<MenuPage>.from(
        (response.data as List).map((json) => MenuPage.fromJson(json)));
    return pages;
  }

  // Récupérer une page par son ID
  Future<MenuPage> getPageById(int id) async {
    final response = await _dio.get('/page/$id');
    return MenuPage.fromJson(response.data);
  }

  // Créer une nouvelle page
  Future<MenuPage> createPage(MenuPage page) async {
    final response = await _dio.post('/page', data: page.toJson());
    return MenuPage.fromJson(response.data);
  }

  // Mettre à jour une page
  Future<MenuPage> updatePage(int id, MenuPage page) async {
    final response = await _dio.put('/page/$id', data: page.toJson());
    return MenuPage.fromJson(response.data);
  }

  // Supprimer une page
  Future<void> deletePage(int id) async {
    await _dio.delete('/page/$id');
  }
}
