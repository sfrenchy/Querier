import 'dart:convert';
import 'dart:io';
import 'package:dio/dio.dart';
import 'package:flutter/foundation.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:dio/io.dart';

class ApiClient {
  final String baseUrl;
  final Dio _dio;
  final FlutterSecureStorage _secureStorage = FlutterSecureStorage();

  static ApiClient? _instance;

  ApiClient._internal(this.baseUrl, this._dio) {
    _dio.options.baseUrl = baseUrl;
    _dio.options.connectTimeout = const Duration(seconds: 5);
    _dio.options.receiveTimeout = const Duration(seconds: 5);

    if (kIsWeb == false) {
      (_dio.httpClientAdapter as IOHttpClientAdapter).createHttpClient = () {
        final client = HttpClient();
        client.badCertificateCallback = (cert, host, port) => true;
        return client;
      };
    }

    _dio.interceptors.add(InterceptorsWrapper(
      onRequest: (options, handler) async {
        final token = await _secureStorage.read(key: 'access_token');
        if (token != null) {
          options.headers['Authorization'] = 'Bearer $token';
        }
        return handler.next(options);
      },
      onError: (DioException error, handler) async {
        if (error.response?.statusCode == 401) {
          final success = await _refreshToken();
          if (success) {
            final token = await _secureStorage.read(key: 'access_token');
            error.requestOptions.headers['Authorization'] = 'Bearer $token';
            final retryResponse = await _dio.request(
              error.requestOptions.path,
              options: Options(
                method: error.requestOptions.method,
                headers: error.requestOptions.headers,
              ),
              data: error.requestOptions.data,
              queryParameters: error.requestOptions.queryParameters,
            );
            return handler.resolve(retryResponse);
          } else {
            await logout();
          }
        }
        return handler.next(error);
      },
    ));
  }

  factory ApiClient({required String baseUrl}) {
    _instance ??= ApiClient._internal(baseUrl, Dio());
    return _instance!;
  }

  // Méthode pour se connecter et stocker les tokens
  Future<bool> signIn(String login, String password) async {
    try {
      final response = await _dio.post('/AuthManagement/SignIn', data: {
        'email': login,
        'password': password,
      });

      if (response.statusCode == 200) {
        final data = response.data;
        await _secureStorage.write(key: 'access_token', value: data['Token']);
        await _secureStorage.write(
            key: 'refresh_token', value: data['RefreshToken']);
        return true;
      } else {
        return false;
      }
    } catch (e) {
      return false;
    }
  }

  Future<bool> _refreshToken() async {
    final refreshToken = await _secureStorage.read(key: 'refresh_token');
    if (refreshToken == null) return false;

    try {
      final response = await _dio.post('/auth/refresh', data: {
        'refresh_token': refreshToken,
      });

      if (response.statusCode == 200) {
        final data = response.data;
        await _secureStorage.write(
            key: 'access_token', value: data['access_token']);
        await _secureStorage.write(
            key: 'refresh_token', value: data['refresh_token']);
        return true;
      } else {
        return false;
      }
    } catch (e) {
      return false;
    }
  }

  Future<void> logout() async {
    await _secureStorage.delete(key: 'access_token');
    await _secureStorage.delete(key: 'refresh_token');
  }

  Future<Response> get(String endpoint, {Map<String, dynamic>? params}) async {
    return await _dio.get(endpoint, queryParameters: params);
  }

  Future<Response> post(String endpoint, Map<String, dynamic> data) async {
    return await _dio.post(endpoint, data: data);
  }

  Future<Response> put(String endpoint, Map<String, dynamic> data) async {
    return await _dio.put(endpoint, data: data);
  }

  Future<Response> delete(String endpoint) async {
    return await _dio.delete(endpoint);
  }
}
