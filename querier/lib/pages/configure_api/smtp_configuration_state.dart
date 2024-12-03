part of 'smtp_configuration_bloc.dart';

abstract class SmtpConfigurationState extends Equatable {
  @override
  List<Object> get props => [];
}

class SmtpConfigurationInitial extends SmtpConfigurationState {}

class SmtpConfigurationLoading extends SmtpConfigurationState {}

class SmtpConfigurationSuccess extends SmtpConfigurationState {}

class SmtpConfigurationFailure extends SmtpConfigurationState {
  final String error;

  SmtpConfigurationFailure(this.error);

  @override
  List<Object> get props => [error];
}

class SmtpConfigurationSuccessWithAuth extends SmtpConfigurationState {
  final Map<String, dynamic> authData;

  SmtpConfigurationSuccessWithAuth(this.authData);

  @override
  List<Object> get props => [authData];
}
