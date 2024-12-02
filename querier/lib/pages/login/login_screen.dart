import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:querier/pages/add_api/add_api_bloc.dart';
import 'package:querier/pages/add_api/add_api_screen.dart';
import 'package:querier/pages/configure_api/admin_configuration_screen.dart';
import 'login_bloc.dart';

class LoginScreen extends StatefulWidget {
  const LoginScreen({super.key});

  @override
  _LoginScreenState createState() => _LoginScreenState();
}

class _LoginScreenState extends State<LoginScreen> {
  final _emailController = TextEditingController();
  final _passwordController = TextEditingController();

  @override
  void dispose() {
    _emailController.dispose();
    _passwordController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final loginBloc = BlocProvider.of<LoginBloc>(context);

    return Scaffold(
      appBar: AppBar(
        title: const Text('Login Screen'),
      ),
      body: MultiBlocListener(
        listeners: [
          BlocListener<LoginBloc, LoginState>(
            listener: (context, state) {
              if (state is LoginFailure) {
                ScaffoldMessenger.of(context)
                  ..hideCurrentSnackBar()
                  ..showSnackBar(SnackBar(
                    content: Text(state.error),
                    duration: const Duration(seconds: 3),
                  ));
              }
            },
          ),
          BlocListener<AddAPIBloc, AddAPIState>(
            listener: (context, state) {
              if (state is APIConfigurationError) {
                ScaffoldMessenger.of(context)
                  ..hideCurrentSnackBar()
                  ..showSnackBar(SnackBar(
                    content: Text(state.message),
                    backgroundColor: Colors.red,
                  ));
              }
            },
          ),
        ],
        child: BlocBuilder<LoginBloc, LoginState>(
          builder: (context, state) {
            return BlocBuilder<AddAPIBloc, AddAPIState>(
              builder: (context, apiState) {
                String? selectedOption;
                List<String> options = [];

                if (state is DropdownAvailableApiSelectedState) {
                  selectedOption = state.selectedUrl;
                  options = state.urls;
                }

                return Padding(
                  padding: const EdgeInsets.all(16.0),
                  child: Column(
                    mainAxisAlignment: MainAxisAlignment.center,
                    children: <Widget>[
                      Row(
                        children: [
                          Expanded(
                            child: DropdownButton<String>(
                              isExpanded: true,
                              value: selectedOption,
                              hint: const Text("API Url"),
                              items: options.map((String apiUrl) {
                                return DropdownMenuItem<String>(
                                  value: apiUrl,
                                  child: Text(apiUrl),
                                );
                              }).toList(),
                              onChanged: (String? newValue) {
                                if (newValue != null) {
                                  loginBloc.add(ApiUrlChangeEvent(newValue));
                                  context.read<AddAPIBloc>().add(
                                      CheckAPIConfigurationEvent(newValue));
                                }
                              },
                            ),
                          ),
                          TextButton(
                            onPressed: () {
                              Navigator.push(
                                context,
                                MaterialPageRoute(
                                  builder: (context) => BlocProvider.value(
                                    value: BlocProvider.of<AddAPIBloc>(context),
                                    child: const AddAPIScreen(),
                                  ),
                                ),
                              );
                            },
                            child: const Text('Add'),
                          )
                        ],
                      ),
                      const SizedBox(height: 16),
                      TextFormField(
                        controller: _emailController,
                        enabled: apiState is! APIConfigurationChecked ||
                            apiState.isConfigured,
                        decoration: const InputDecoration(
                          labelText: 'Email',
                          border: OutlineInputBorder(),
                        ),
                      ),
                      const SizedBox(height: 16),
                      TextFormField(
                        controller: _passwordController,
                        enabled: apiState is! APIConfigurationChecked ||
                            apiState.isConfigured,
                        obscureText: true,
                        decoration: const InputDecoration(
                          labelText: 'Password',
                          border: OutlineInputBorder(),
                        ),
                      ),
                      const SizedBox(height: 16),
                      if (apiState is APIConfigurationChecked &&
                          !apiState.isConfigured)
                        SizedBox(
                          width: double.infinity,
                          child: ElevatedButton(
                            onPressed: () {
                              Navigator.push(
                                context,
                                MaterialPageRoute(
                                  builder: (context) =>
                                      const AdminConfigurationScreen(),
                                ),
                              );
                            },
                            child: const Text('Configure API'),
                          ),
                        ),
                      SizedBox(
                        width: double.infinity,
                        child: ElevatedButton(
                          onPressed: (apiState is! APIConfigurationChecked ||
                                      apiState.isConfigured) &&
                                  state is! LoginLoading
                              ? () {
                                  loginBloc.add(
                                    LoginButtonPressed(
                                      apiUrl: selectedOption ?? "",
                                      email: _emailController.text,
                                      password: _passwordController.text,
                                    ),
                                  );
                                }
                              : null,
                          child: const Text('Login'),
                        ),
                      ),
                      if (state is LoginLoading)
                        const Padding(
                          padding: EdgeInsets.all(8.0),
                          child: CircularProgressIndicator(),
                        ),
                    ],
                  ),
                );
              },
            );
          },
        ),
      ),
    );
  }
}
