class Validators {
  static bool isValidEmail(String email) {
    return RegExp(r'^[\w-\.]+@([\w-]+\.)+[\w-]{2,4}$').hasMatch(email);
  }

  static bool isValidPort(String port) {
    if (port.isEmpty) return false;

    final number = int.tryParse(port);
    if (number == null) return false;

    return number > 0 && number <= 65535; // Valid port range
  }

  // You can add more validation methods here
  // For example:
  // static bool isValidPassword(String password) { ... }
  // static bool isValidPhoneNumber(String phone) { ... }
}
