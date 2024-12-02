import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'smtp_configuration_screen.dart';
import 'admin_configuration_bloc.dart';
import 'package:querier/utils/validators.dart';

class AdminConfigurationScreen extends StatefulWidget {
  const AdminConfigurationScreen({super.key});

  @override
  State<AdminConfigurationScreen> createState() =>
      _AdminConfigurationScreenState();
}

class _AdminConfigurationScreenState extends State<AdminConfigurationScreen> {
  final _nameController = TextEditingController();
  final _firstNameController = TextEditingController();
  final _emailController = TextEditingController();
  final _passwordController = TextEditingController();
  bool _isFormValid = false;

  @override
  void initState() {
    super.initState();
    // Add listeners to all controllers
    _nameController.addListener(_validateForm);
    _firstNameController.addListener(_validateForm);
    _emailController.addListener(_validateForm);
    _passwordController.addListener(_validateForm);
  }

  @override
  void dispose() {
    _nameController.dispose();
    _firstNameController.dispose();
    _emailController.dispose();
    _passwordController.dispose();
    super.dispose();
  }

  void _validateForm() {
    setState(() {
      _isFormValid = _nameController.text.isNotEmpty &&
          _firstNameController.text.isNotEmpty &&
          Validators.isValidEmail(_emailController.text) &&
          _passwordController.text.isNotEmpty;
    });
  }

  @override
  Widget build(BuildContext context) {
    return BlocProvider(
      create: (context) => AdminConfigurationBloc(),
      child: BlocConsumer<AdminConfigurationBloc, AdminConfigurationState>(
        listener: (context, state) {
          if (state is AdminConfigurationSuccess) {
            Navigator.push(
              context,
              MaterialPageRoute(
                builder: (context) => const SMTPConfigurationScreen(),
              ),
            );
          } else if (state is AdminConfigurationFailure) {
            ScaffoldMessenger.of(context).showSnackBar(
              SnackBar(content: Text(state.error)),
            );
          }
        },
        builder: (context, state) {
          return Scaffold(
            appBar: AppBar(
              title: const Text('Configure SuperAdmin'),
            ),
            body: Padding(
              padding: const EdgeInsets.all(16.0),
              child: Column(
                children: [
                  TextFormField(
                    controller: _nameController,
                    decoration: const InputDecoration(
                      labelText: 'Name',
                      border: OutlineInputBorder(),
                    ),
                  ),
                  const SizedBox(height: 16),
                  TextFormField(
                    controller: _firstNameController,
                    decoration: const InputDecoration(
                      labelText: 'First Name',
                      border: OutlineInputBorder(),
                    ),
                  ),
                  const SizedBox(height: 16),
                  TextFormField(
                    controller: _emailController,
                    decoration: InputDecoration(
                      labelText: 'Email',
                      border: const OutlineInputBorder(),
                      errorText: _emailController.text.isNotEmpty &&
                              !Validators.isValidEmail(_emailController.text)
                          ? 'Please enter a valid email'
                          : null,
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
                  SizedBox(
                    width: double.infinity,
                    child: ElevatedButton(
                      onPressed:
                          (state is! AdminConfigurationLoading && _isFormValid)
                              ? () {
                                  context.read<AdminConfigurationBloc>().add(
                                        SubmitConfigurationEvent(
                                          name: _nameController.text,
                                          firstName: _firstNameController.text,
                                          email: _emailController.text,
                                          password: _passwordController.text,
                                        ),
                                      );
                                }
                              : null,
                      child: state is AdminConfigurationLoading
                          ? const CircularProgressIndicator()
                          : const Text('Next'),
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
