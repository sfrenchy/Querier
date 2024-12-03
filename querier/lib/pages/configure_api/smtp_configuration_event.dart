part of 'smtp_configuration_bloc.dart';

abstract class SmtpConfigurationEvent extends Equatable {
  @override
  List<Object> get props => [];
}

class SubmitSmtpConfigurationEvent extends SmtpConfigurationEvent {
  final String adminName;
  final String adminFirstName;
  final String adminEmail;
  final String adminPassword;
  final String host;
  final String port;
  final String username;
  final String password;
  final bool useSSL;

  SubmitSmtpConfigurationEvent({
    required this.adminName,
    required this.adminFirstName,
    required this.adminEmail,
    required this.adminPassword,
    required this.host,
    required this.port,
    required this.username,
    required this.password,
    required this.useSSL,
  });

  @override
  List<Object> get props => [
        adminName,
        adminFirstName,
        adminEmail,
        adminPassword,
        host,
        port,
        username,
        password,
        useSSL
      ];
}
