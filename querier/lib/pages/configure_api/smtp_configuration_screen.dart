import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'smtp_configuration_bloc.dart';
import 'package:querier/utils/validators.dart';
import 'package:flutter/services.dart';
import 'package:querier/services/wizard_service.dart';

class SMTPConfigurationScreen extends StatefulWidget {
  final String apiUrl;
  final String adminName;
  final String adminFirstName;
  final String adminEmail;
  final String adminPassword;

  const SMTPConfigurationScreen({
    super.key,
    required this.apiUrl,
    required this.adminName,
    required this.adminFirstName,
    required this.adminEmail,
    required this.adminPassword,
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
  bool _useSSL = true;
  bool _isFormValid = false;

  @override
  void initState() {
    super.initState();
    _hostController.addListener(_validateForm);
    _portController.addListener(_validateForm);
    _usernameController.addListener(_validateForm);
    _passwordController.addListener(_validateForm);
  }

  void _validateForm() {
    setState(() {
      _isFormValid = _hostController.text.isNotEmpty &&
          Validators.isValidPort(_portController.text) &&
          _usernameController.text.isNotEmpty &&
          _passwordController.text.isNotEmpty;
    });
  }

  @override
  void dispose() {
    _hostController.dispose();
    _portController.dispose();
    _usernameController.dispose();
    _passwordController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return BlocProvider(
      create: (context) => SmtpConfigurationBloc(
        WizardService(widget.apiUrl),
      ),
      child: BlocConsumer<SmtpConfigurationBloc, SmtpConfigurationState>(
        listener: (context, state) {
          if (state is SmtpConfigurationSuccessWithAuth) {
            Navigator.of(context).pushReplacementNamed('/home');
          } else if (state is SmtpConfigurationSuccess) {
            ScaffoldMessenger.of(context).showSnackBar(
              const SnackBar(
                  content: Text('SMTP Configuration saved successfully')),
            );
            Navigator.of(context).pushReplacementNamed('/login');
          } else if (state is SmtpConfigurationFailure) {
            ScaffoldMessenger.of(context).showSnackBar(
              SnackBar(content: Text(state.error)),
            );
          }
        },
        builder: (context, state) {
          return Scaffold(
            appBar: AppBar(
              title: const Text('SMTP Configuration'),
            ),
            body: Padding(
              padding: const EdgeInsets.all(16.0),
              child: Column(
                children: [
                  TextFormField(
                    controller: _hostController,
                    decoration: const InputDecoration(
                      labelText: 'SMTP Host',
                      border: OutlineInputBorder(),
                    ),
                  ),
                  const SizedBox(height: 16),
                  TextFormField(
                    controller: _portController,
                    keyboardType: TextInputType.number,
                    inputFormatters: [
                      FilteringTextInputFormatter.digitsOnly,
                    ],
                    decoration: const InputDecoration(
                      labelText: 'SMTP Port',
                      border: OutlineInputBorder(),
                      helperText: 'Enter a port number between 1-65535',
                    ),
                    validator: (value) => !Validators.isValidPort(value ?? '')
                        ? 'Please enter a valid port (1-65535)'
                        : null,
                    autovalidateMode: AutovalidateMode.onUserInteraction,
                  ),
                  const SizedBox(height: 16),
                  TextFormField(
                    controller: _usernameController,
                    decoration: const InputDecoration(
                      labelText: 'Username',
                      border: OutlineInputBorder(),
                    ),
                  ),
                  const SizedBox(height: 16),
                  TextFormField(
                    controller: _passwordController,
                    obscureText: true,
                    decoration: const InputDecoration(
                      labelText: 'Password',
                      border: OutlineInputBorder(),
                    ),
                  ),
                  const SizedBox(height: 16),
                  SwitchListTile(
                    title: const Text('Use SSL'),
                    value: _useSSL,
                    onChanged: (bool value) {
                      setState(() {
                        _useSSL = value;
                      });
                    },
                  ),
                  const SizedBox(height: 16),
                  SizedBox(
                    width: double.infinity,
                    child: ElevatedButton(
                      onPressed:
                          !_isFormValid || state is SmtpConfigurationLoading
                              ? null
                              : () {
                                  context.read<SmtpConfigurationBloc>().add(
                                        SubmitSmtpConfigurationEvent(
                                          adminName: widget.adminName,
                                          adminFirstName: widget.adminFirstName,
                                          adminEmail: widget.adminEmail,
                                          adminPassword: widget.adminPassword,
                                          host: _hostController.text,
                                          port: _portController.text,
                                          username: _usernameController.text,
                                          password: _passwordController.text,
                                          useSSL: _useSSL,
                                        ),
                                      );
                                },
                      child: state is SmtpConfigurationLoading
                          ? const CircularProgressIndicator()
                          : const Text('Save Configuration'),
                    ),
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
