part of 'login_bloc.dart';

abstract class LoginEvent extends Equatable {
  const LoginEvent();

  @override
  List<Object> get props => [];
}

class ApiUrlChangeEvent extends LoginEvent {
  final AvailableApiUrl selectedApiUrl;

  ApiUrlChangeEvent(this.selectedApiUrl);
  @override
  String toString() => 'ApiUrlOptionEvent change [id: $selectedApiUrl.id]';
}

class LoginButtonPressed extends LoginEvent {
  final String apiUrl;
  final String email;
  final String password;

  const LoginButtonPressed(
      {required this.apiUrl, required this.email, required this.password});

  @override
  List<Object> get props => [apiUrl, email, password];

  @override
  String toString() =>
      'LoginButtonPressed { apiUrl: $apiUrl, email: $email, password: $password }';
}
