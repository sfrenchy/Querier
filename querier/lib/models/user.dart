class User {
  final String id;
  final String email;
  final String firstName;
  final String lastName;
  final String userName;
  final List<String> roles;

  User({
    required this.id,
    required this.email,
    required this.firstName,
    required this.lastName,
    required this.userName,
    required this.roles,
  });

  factory User.fromJson(Map<String, dynamic> json) {
    return User(
      id: json['Id'] ?? '',
      email: json['Email'] ?? '',
      firstName: json['FirstName'] ?? '',
      lastName: json['LastName'] ?? '',
      userName: json['UserName'] ?? '',
      roles: List<String>.from(json['Roles'] ?? []),
    );
  }
}
