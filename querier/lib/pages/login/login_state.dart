part of 'login_bloc.dart';

abstract class LoginState extends Equatable {
  const LoginState();

  @override
  List<Object> get props => [];
}

class DropdownAvailableApiSelectedState extends LoginState {
  final List<String> urls;
  final String selectedUrl;

  const DropdownAvailableApiSelectedState(this.urls, this.selectedUrl);

  @override
  List<Object> get props => [urls, selectedUrl];
}

class LoginInitial extends LoginState {}

class LoginLoading extends LoginState {}

class LoginSuccess extends LoginState {}

class LoginFailure extends LoginState {
  final String error;

  const LoginFailure({required this.error});

  @override
  List<Object> get props => [error];

  @override
  String toString() => 'LoginFailure { error: $error }';
}
