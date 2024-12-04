import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_gen/gen_l10n/app_localizations.dart';
import 'smtp_configuration_bloc.dart';
import 'package:querier/api/api_client.dart';

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
  final _hostController = TextEditingController();
  final _portController = TextEditingController();
  final _usernameController = TextEditingController();
  final _passwordController = TextEditingController();
  final _senderEmailController = TextEditingController();
  final _senderNameController = TextEditingController();
  bool _useSsl = true;

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context)!;

    return BlocProvider(
      create: (context) => SmtpConfigurationBloc(widget.apiUrl),
      child: BlocConsumer<SmtpConfigurationBloc, SmtpConfigurationState>(
        listener: (context, state) {
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
              child: Column(
                children: [
                  TextFormField(
                    controller: _hostController,
                    decoration: InputDecoration(
                      labelText: l10n.smtpHost,
                      border: const OutlineInputBorder(),
                    ),
                  ),
                  const SizedBox(height: 16),
                  TextFormField(
                    controller: _portController,
                    decoration: InputDecoration(
                      labelText: l10n.smtpPort,
                      border: const OutlineInputBorder(),
                    ),
                    keyboardType: TextInputType.number,
                    inputFormatters: [FilteringTextInputFormatter.digitsOnly],
                  ),
                  const SizedBox(height: 16),
                  TextFormField(
                    controller: _usernameController,
                    decoration: InputDecoration(
                      labelText: l10n.smtpUsername,
                      border: const OutlineInputBorder(),
                    ),
                  ),
                  const SizedBox(height: 16),
                  TextFormField(
                    controller: _passwordController,
                    decoration: InputDecoration(
                      labelText: l10n.smtpPassword,
                      border: const OutlineInputBorder(),
                    ),
                    obscureText: true,
                  ),
                  const SizedBox(height: 16),
                  TextFormField(
                    controller: _senderEmailController,
                    decoration: InputDecoration(
                      labelText: l10n.smtpSenderEmail,
                      border: const OutlineInputBorder(),
                    ),
                  ),
                  const SizedBox(height: 16),
                  TextFormField(
                    controller: _senderNameController,
                    decoration: InputDecoration(
                      labelText: l10n.smtpSenderName,
                      border: const OutlineInputBorder(),
                    ),
                  ),
                  const SizedBox(height: 16),
                  SwitchListTile(
                    title: Text(l10n.useSsl),
                    value: _useSsl,
                    onChanged: (bool value) {
                      setState(() {
                        _useSsl = value;
                      });
                    },
                  ),
                  const SizedBox(height: 16),
                  Row(
                    children: [
                      Expanded(
                        child: ElevatedButton(
                          onPressed: state is SmtpConfigurationLoading
                              ? null
                              : () {
                                  context.read<SmtpConfigurationBloc>().add(
                                        TestSmtpConfigurationEvent(
                                          host: _hostController.text,
                                          port: int.tryParse(
                                                  _portController.text) ??
                                              0,
                                          username: _usernameController.text,
                                          password: _passwordController.text,
                                          senderEmail:
                                              _senderEmailController.text,
                                          senderName:
                                              _senderNameController.text,
                                          useSsl: _useSsl,
                                        ),
                                      );
                                },
                          child: state is SmtpTestLoading
                              ? Text(l10n.testingConnection)
                              : Text(l10n.testConnection),
                        ),
                      ),
                      const SizedBox(width: 16),
                      Expanded(
                        child: ElevatedButton(
                          onPressed: state is SmtpConfigurationLoading
                              ? null
                              : () {
                                  context.read<SmtpConfigurationBloc>().add(
                                        SubmitSmtpConfigurationEvent(
                                          adminName: widget.adminName,
                                          adminFirstName: widget.adminFirstName,
                                          adminEmail: widget.adminEmail,
                                          adminPassword: widget.adminPassword,
                                          apiUrl: widget.apiUrl,
                                          host: _hostController.text,
                                          port: int.tryParse(
                                                  _portController.text) ??
                                              0,
                                          username: _usernameController.text,
                                          password: _passwordController.text,
                                          useSSL: _useSsl,
                                          senderEmail:
                                              _senderEmailController.text,
                                          senderName:
                                              _senderNameController.text,
                                        ),
                                      );
                                },
                          child: state is SmtpConfigurationLoading
                              ? const CircularProgressIndicator()
                              : Text(l10n.finish),
                        ),
                      ),
                    ],
                  ),
                ],
              ),
            ),
          );
        },
      ),
    );
  }
}
