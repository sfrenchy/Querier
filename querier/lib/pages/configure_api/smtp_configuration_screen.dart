import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_gen/gen_l10n/app_localizations.dart';
import 'smtp_configuration_bloc.dart';
import 'package:querier/api/api_client.dart';
import 'package:querier/widgets/smtp_configuration_form.dart';

class SMTPConfigurationScreen extends StatefulWidget {
  final String adminName;
  final String adminFirstName;
  final String adminEmail;
  final String adminPassword;
  final String apiUrl;

  const SMTPConfigurationScreen({
    super.key,
    required this.adminName,
    required this.adminFirstName,
    required this.adminEmail,
    required this.adminPassword,
    required this.apiUrl,
  });

  @override
  State<SMTPConfigurationScreen> createState() =>
      _SMTPConfigurationScreenState();
}

class _SMTPConfigurationScreenState extends State<SMTPConfigurationScreen> {
  final _formKey = GlobalKey<FormState>();
  bool _useSsl = true;

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context)!;

    return BlocProvider(
      create: (context) =>
          SmtpConfigurationBloc(widget.apiUrl, Navigator.of(context)),
      child: BlocConsumer<SmtpConfigurationBloc, SmtpConfigurationState>(
        listener: (context, state) async {
          if (state is SmtpConfigurationSuccess) {
            Navigator.pushReplacementNamed(context, '/home');
          } else if (state is SmtpConfigurationFailure) {
            ScaffoldMessenger.of(context).showSnackBar(
              SnackBar(content: Text(state.error)),
            );
          } else if (state is SmtpTestSuccess) {
            ScaffoldMessenger.of(context).showSnackBar(
              SnackBar(content: Text(l10n.connectionSuccess)),
            );
          } else if (state is SmtpTestFailure) {
            ScaffoldMessenger.of(context).showSnackBar(
              SnackBar(content: Text(l10n.connectionFailed)),
            );
          } else if (state is SmtpConfigurationSuccessWithAuth) {
            final token = state.authResponse['Token'];
            final refreshToken = state.authResponse['RefreshToken'];

            context.read<ApiClient>().setAuthToken(token);
            await context.read<ApiClient>().storeRefreshToken(refreshToken);

            Navigator.of(context).pushNamedAndRemoveUntil(
              '/home',
              (route) => false,
            );
          }
        },
        builder: (context, state) {
          return Scaffold(
            appBar: AppBar(
              title: Text(l10n.smtpConfiguration),
            ),
            body: Padding(
              padding: const EdgeInsets.all(16.0),
              child: SmtpConfigurationForm(
                formKey: _formKey,
                showTestButton: true,
                onTest: () {
                  if (_formKey.currentState!.validate()) {
                    // TODO: Implémenter le test
                  }
                },
                onSaveValues: (host, port, username, password, useSSL,
                    senderEmail, senderName, requireAuth) {
                  context.read<SmtpConfigurationBloc>().add(
                        SubmitSmtpConfigurationEvent(
                          adminName: widget.adminName,
                          adminFirstName: widget.adminFirstName,
                          adminEmail: widget.adminEmail,
                          adminPassword: widget.adminPassword,
                          apiUrl: widget.apiUrl,
                          host: host,
                          port: port,
                          username: username,
                          password: password,
                          useSSL: useSSL,
                          senderEmail: senderEmail,
                          senderName: senderName,
                          requireAuth: requireAuth,
                        ),
                      );
                },
              ),
            ),
          );
        },
      ),
    );
  }
}
