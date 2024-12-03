import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:querier/pages/add_api/add_api_screen.dart';
import 'package:querier/pages/configure_api/admin_configuration_screen.dart';
import 'login_bloc.dart';

class LoginScreen extends StatelessWidget {
  final _emailController = TextEditingController();
  final _passwordController = TextEditingController();

  LoginScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Login'),
      ),
      body: BlocConsumer<LoginBloc, LoginState>(
        listener: (context, state) {
          if (state.error != null) {
            ScaffoldMessenger.of(context).showSnackBar(
              SnackBar(content: Text(state.error!)),
            );
          }
          if (state.isAuthenticated) {
            Navigator.pushReplacementNamed(context, '/home');
          }
        },
        builder: (context, state) {
          return Padding(
            padding: const EdgeInsets.all(16.0),
            child: Column(
              mainAxisAlignment: MainAxisAlignment.center,
              children: [
                _buildApiUrlDropdown(context, state),
                const SizedBox(height: 24),
                if (state.isConfigured) ...[
                  _buildEmailField(),
                  const SizedBox(height: 16),
                  _buildPasswordField(),
                  const SizedBox(height: 32),
                  _buildLoginButton(context, state),
                ] else if (state.selectedUrl.isNotEmpty) ...[
                  const SizedBox(height: 24),
                  _buildConfigureButton(context),
                ],
                if (state.isLoading)
                  const Padding(
                    padding: EdgeInsets.all(8.0),
                    child: CircularProgressIndicator(),
                  ),
              ],
            ),
          );
        },
      ),
    );
  }

  Widget _buildApiUrlDropdown(BuildContext context, LoginState state) {
    return Container(
      decoration: BoxDecoration(
        border: Border.all(color: Colors.grey),
        borderRadius: BorderRadius.circular(4),
      ),
      padding: const EdgeInsets.symmetric(horizontal: 12),
      child: Row(
        children: [
          Expanded(
            child: DropdownButtonHideUnderline(
              child: DropdownButton<String>(
                isExpanded: true,
                value: state.selectedUrl.isEmpty ? null : state.selectedUrl,
                hint: const Text('Select API URL'),
                items: state.urls.map((url) {
                  return DropdownMenuItem(
                    value: url,
                    child: Text(url),
                  );
                }).toList(),
                onChanged: (newValue) {
                  if (newValue != null) {
                    context.read<LoginBloc>().add(UrlChanged(newValue));
                  }
                },
              ),
            ),
          ),
          IconButton(
            icon: const Icon(Icons.add),
            onPressed: () {
              Navigator.push(
                context,
                MaterialPageRoute(builder: (context) => AddApiScreen()),
              );
            },
          ),
        ],
      ),
    );
  }

  Widget _buildEmailField() {
    return TextFormField(
      controller: _emailController,
      decoration: const InputDecoration(
        labelText: 'Email',
        border: OutlineInputBorder(),
        prefixIcon: Icon(Icons.email),
      ),
      keyboardType: TextInputType.emailAddress,
      textInputAction: TextInputAction.next,
    );
  }

  Widget _buildPasswordField() {
    return TextFormField(
      controller: _passwordController,
      decoration: const InputDecoration(
        labelText: 'Password',
        border: OutlineInputBorder(),
        prefixIcon: Icon(Icons.lock),
      ),
      obscureText: true,
      textInputAction: TextInputAction.done,
    );
  }

  Widget _buildLoginButton(BuildContext context, LoginState state) {
    return SizedBox(
      width: double.infinity,
      height: 48,
      child: ElevatedButton(
        onPressed: state.isLoading
            ? null
            : () {
                context.read<LoginBloc>().add(
                      LoginSubmitted(
                        email: _emailController.text,
                        password: _passwordController.text,
                      ),
                    );
              },
        style: ElevatedButton.styleFrom(
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(8),
          ),
        ),
        child: state.isLoading
            ? const SizedBox(
                height: 20,
                width: 20,
                child: CircularProgressIndicator(
                  strokeWidth: 2,
                ),
              )
            : const Text(
                'Login',
                style: TextStyle(fontSize: 16),
              ),
      ),
    );
  }

  Widget _buildConfigureButton(BuildContext context) {
    return SizedBox(
      width: double.infinity,
      height: 48,
      child: ElevatedButton(
        onPressed: () {
          Navigator.push(
            context,
            MaterialPageRoute(
              builder: (context) => AdminConfigurationScreen(
                apiUrl: context.read<LoginBloc>().state.selectedUrl,
              ),
            ),
          );
        },
        style: ElevatedButton.styleFrom(
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(8),
          ),
        ),
        child: const Text('Configure API'),
      ),
    );
  }
}
