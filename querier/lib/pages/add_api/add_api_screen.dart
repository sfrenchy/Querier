import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:querier/pages/add_api/add_api_bloc.dart';
import 'package:querier/pages/login/login_bloc.dart';

class AddAPIScreen extends StatefulWidget {
  const AddAPIScreen({super.key});

  @override
  _AddAPIScreenState createState() => _AddAPIScreenState();
}

class _AddAPIScreenState extends State<AddAPIScreen> {
  final _hostController = TextEditingController();
  final _portController = TextEditingController();
  final _urlPathController = TextEditingController();
  final _apiURL = TextEditingController();

  @override
  void dispose() {
    _hostController.dispose();
    _portController.dispose();
    _urlPathController.dispose();
    _apiURL.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final addAPIBloc = BlocProvider.of<AddAPIBloc>(context);
    return Scaffold(
      appBar: AppBar(
        title: const Text('Add API Screen'),
      ),
      body: BlocListener<AddAPIBloc, AddAPIState>(
        listener: (context, state) {
          if (state is AddAPISaveSuccess) {
            // Remplacez par un état approprié
            // Déclencher le rafraîchissement dans LoginBloc
            context.read<LoginBloc>().add(RefreshApiUrlsEvent());
          }
        },
        child: BlocBuilder<AddAPIBloc, AddAPIState>(
          builder: (context, state) {
            String selectedProtocol =
                addAPIBloc.selectedProtocol; // Valeur par défaut
            List<String> protocols = addAPIBloc.protocols; // Valeur par défaut

            if (state is DropdownProtocolSelectedState) {
              selectedProtocol = state.selectedProtocol;
              protocols = state.protocols;
            }
            if (state is AddAPIInitial) {
              _hostController.text = state.host;
              _portController.text = state.port.toString();
              _urlPathController.text = state.urlPath;
            }
            if (state is AddAPIURL) {
              _apiURL.text = state.apiURL;
            }
            return Padding(
              padding: const EdgeInsets.all(16.0),
              child: Column(
                  mainAxisAlignment: MainAxisAlignment.center,
                  children: <Widget>[
                    Row(
                      children: [
                        const Text(
                          "Protocol:",
                          textAlign: TextAlign.left,
                        ),
                        const SizedBox(width: 16.0),
                        Expanded(
                          child: DropdownButton<String>(
                            isExpanded: true,
                            value: selectedProtocol,
                            items: protocols.map((String protocol) {
                              return DropdownMenuItem<String>(
                                value: protocol,
                                child: Text(protocol), // Utiliser le label ici
                              );
                            }).toList(),
                            onChanged: (String? newValue) {
                              if (newValue != null) {
                                addAPIBloc
                                    .add(AddAPIProtocolChangeEvent(newValue));
                              }
                            },
                          ),
                        ),
                        const SizedBox(height: 16.0),
                      ],
                    ),
                    Row(
                      children: [
                        const Text(
                          "Host:",
                          textAlign: TextAlign.left,
                        ),
                        const SizedBox(width: 16.0),
                        Expanded(
                          child: TextFormField(
                            controller: _hostController,
                            decoration: const InputDecoration(
                              labelText: 'Host',
                              border: OutlineInputBorder(),
                            ),
                            onChanged: (value) {
                              addAPIBloc.add(AddAPIHostChangeEvent(value));
                            },
                          ),
                        ),
                      ],
                    ),
                    const SizedBox(height: 16.0),
                    Row(
                      children: [
                        const Text(
                          "Port:",
                          textAlign: TextAlign.left,
                        ),
                        const SizedBox(width: 16.0),
                        Expanded(
                          child: TextFormField(
                            controller: _portController,
                            decoration: const InputDecoration(
                              labelText: 'Port',
                              border: OutlineInputBorder(),
                            ),
                            keyboardType: TextInputType.number,
                            inputFormatters: <TextInputFormatter>[
                              FilteringTextInputFormatter.digitsOnly
                            ],
                            onChanged: (value) {
                              addAPIBloc
                                  .add(AddAPIPortChangeEvent(int.parse(value)));
                            },
                          ),
                        ),
                      ],
                    ),
                    const SizedBox(height: 16.0),
                    Row(
                      children: [
                        const Text(
                          "URL Path:",
                          textAlign: TextAlign.left,
                        ),
                        const SizedBox(width: 16.0),
                        Expanded(
                          child: TextFormField(
                            controller: _urlPathController,
                            decoration: const InputDecoration(
                              labelText: 'URL Path',
                              border: OutlineInputBorder(),
                            ),
                            onChanged: (value) {
                              addAPIBloc.add(AddAPIURLPathChangeEvent(value));
                            },
                          ),
                        ),
                      ],
                    ),
                    const SizedBox(height: 16.0),
                    Row(
                      children: [
                        const Text(
                          "API URL:",
                          textAlign: TextAlign.left,
                        ),
                        const SizedBox(width: 16.0),
                        Expanded(
                          child: Text(
                            _apiURL.text,
                            textAlign: TextAlign.left,
                          ),
                        ),
                      ],
                    ),
                    const SizedBox(height: 32.0),
                    Row(
                      children: [
                        Expanded(
                          child: TextButton(
                            onPressed: () {
                              Navigator.pop(context);
                            },
                            child: const Text('Cancel'),
                          ),
                        ),
                        Expanded(
                          child: TextButton(
                            onPressed: () {
                              addAPIBloc.add(AddAPISaveEvent());
                              Navigator.pop(context);
                            },
                            child: const Text('Save'),
                          ),
                        )
                      ],
                    ),
                  ]),
            );
          },
        ),
      ),
    );
  }
}
