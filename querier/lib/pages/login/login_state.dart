part of 'login_bloc.dart';

abstract class DropdownState {}

class DropdownOptionSelectedState extends LoginState {
  final List<AvailableApiUrl> options; // Changer ici
  final AvailableApiUrl selectedOption; // Changer ici

  const DropdownOptionSelectedState(this.options, this.selectedOption);

  @override
  List<Object> get props => [
        options,
        selectedOption
      ]; // Ajoutez cela pour que l'état soit équitablement comparé.
}

abstract class LoginState extends Equatable {
  const LoginState();

  @override
  List<Object> get props => [];
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
