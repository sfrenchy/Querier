class User {
  final String id;
  final String email;
  final String firstName;
  final String lastName;
  final String userName;
  final List<String> roles;
  final List<String> selectedRoles;
  final bool isEmailConfirmed;

  User({
    required this.id,
    required this.email,
    required this.firstName,
    required this.lastName,
    required this.userName,
    required this.roles,
    this.selectedRoles = const [],
    required this.isEmailConfirmed,
  });

  factory User.fromJson(Map<String, dynamic> json) {
    return User(
      id: json['Id'] ?? '',
      email: json['Email'] ?? '',
      firstName: json['FirstName'] ?? '',
      lastName: json['LastName'] ?? '',
      userName: json['UserName'] ?? '',
      roles: List<String>.from(json['Roles'] ?? []),
      selectedRoles: List<String>.from(json['Roles'] ?? []),
      isEmailConfirmed: json['IsEmailConfirmed'] ?? false,
    );
  }
}
