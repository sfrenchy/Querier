import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:querier/model/available_api_url.dart';
import 'package:querier/pages/add_api/add_api_bloc.dart';
import 'package:querier/pages/add_api/add_api_screen.dart';
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
      body: BlocListener<LoginBloc, LoginState>(
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
        child: BlocBuilder<LoginBloc, LoginState>(
          builder: (context, state) {
            AvailableApiUrl? selectedOption;
            List<AvailableApiUrl> options = [];

            // Vérification de l'état actuel pour récupérer les options et la sélection
            if (state is DropdownOptionSelectedState) {
              selectedOption = state.selectedUrl;
              options = state.urls;
              print(
                  "Current selected option in UI: $selectedOption"); // Debugging line
            }

            return Padding(
              padding: const EdgeInsets.all(16.0),
              child: Column(
                mainAxisAlignment: MainAxisAlignment.center,
                children: <Widget>[
                  Row(
                    children: [
                      Expanded(
                        child: DropdownButton<AvailableApiUrl>(
                          isExpanded: true,
                          value: selectedOption,
                          hint: const Text("Sélectionnez un serveur"),
                          items: options.map((AvailableApiUrl apiUrl) {
                            return DropdownMenuItem<AvailableApiUrl>(
                              value: apiUrl,
                              child: Text(apiUrl.url), // Utiliser le label ici
                            );
                          }).toList(),
                          onChanged: (AvailableApiUrl? newValue) {
                            if (newValue != null) {
                              loginBloc.add(ApiUrlChangeEvent(newValue));
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
                    decoration: const InputDecoration(
                      labelText: 'Email',
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
                  ElevatedButton(
                    onPressed: state is! LoginLoading
                        ? () {
                            loginBloc.add(
                              LoginButtonPressed(
                                apiUrl: selectedOption?.url ?? "",
                                email: _emailController.text,
                                password: _passwordController.text,
                              ),
                            );
                          }
                        : null,
                    child: const Text('Login'),
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
        ),
      ),
    );
  }
}
