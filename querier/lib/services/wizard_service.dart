import 'package:dio/dio.dart';

class WizardService {
  final Dio _dio;
  final String baseUrl;

  WizardService(this.baseUrl) : _dio = Dio();

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
    try {
      final response = await _dio.post(
        '$baseUrl/wizard/setup',
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
    } catch (e) {
      throw Exception('Failed to setup: ${e.toString()}');
    }
  }
}
