class ApiEndpoints {
  // Auth endpoints
  static const String signIn = '/authmanagement/signin';
  static const String signOut = '/authmanagement/signout';
  static const String refreshToken = '/authmanagement/refreshtoken';

  // Settings endpoints
  static const String getSettings = '/settings';
  static const String updateSettings = '/settings';
  static const String isConfigured = '/settings/configured';
  static const String configure = '/settings/configure';

  // Wizard endpoints
  static const String setup = '/wizard/setup';

  // User endpoints
  static const String users = '/users';
  static const String currentUser = '/usermanagement/me';
  static const String userById = '/users/{id}';

  // Role endpoints
  static const String roles = '/roles';
  static const String roleById = '/roles/{id}';

  // Helper method pour construire les URLs complètes
  static String buildUrl(String baseUrl, String endpoint) {
    // Enlever les slashes en trop
    final cleanBaseUrl = baseUrl.endsWith('/')
        ? baseUrl.substring(0, baseUrl.length - 1)
        : baseUrl;
    final cleanEndpoint =
        endpoint.startsWith('/') ? endpoint.substring(1) : endpoint;

    return '$cleanBaseUrl/$cleanEndpoint';
  }

  // Helper pour les URLs avec paramètres
  static String replaceUrlParams(String endpoint, Map<String, String> params) {
    String result = endpoint;
    params.forEach((key, value) {
      result = result.replaceAll('{$key}', value);
    });
    return result;
  }

  // User endpoints
  static const String userProfile = '/usermanagement/view/{id}';
  static const String recentQueries = '/queries/recent';
  static const String queryStats = '/queries/stats';
  static const String activity = '/queries/activity';
}
