part of 'smtp_configuration_bloc.dart';

abstract class SmtpConfigurationEvent extends Equatable {
  @override
  List<Object> get props => [];
}

class SubmitSmtpConfigurationEvent extends SmtpConfigurationEvent {
  final String host;
  final String port;
  final String username;
  final String password;
  final bool useSSL;

  SubmitSmtpConfigurationEvent({
    required this.host,
    required this.port,
    required this.username,
    required this.password,
    required this.useSSL,
  });

  @override
  List<Object> get props => [host, port, username, password, useSSL];
}
