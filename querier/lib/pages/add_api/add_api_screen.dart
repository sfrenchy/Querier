import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:querier/pages/login/login_bloc.dart';
import 'add_api_bloc.dart';

class AddApiScreen extends StatelessWidget {
  final _hostController = TextEditingController();
  final _portController = TextEditingController();
  final _urlPathController = TextEditingController();

  AddApiScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return BlocProvider(
      create: (context) => AddApiBloc(),
      child: BlocConsumer<AddApiBloc, AddApiState>(
        listener: (context, state) {
          if (state is AddApiSuccess) {
            context.read<LoginBloc>().add(LoadSavedUrls());
            Navigator.pop(context);
          } else if (state is AddApiError) {
            ScaffoldMessenger.of(context).showSnackBar(
              SnackBar(content: Text(state.message)),
            );
          }
        },
        builder: (context, state) {
          return Scaffold(
            appBar: AppBar(
              title: const Text('Add API'),
              actions: [
                IconButton(
                  icon: const Icon(Icons.save),
                  onPressed: () {
                    context.read<AddApiBloc>().add(SaveApiUrl());
                  },
                ),
              ],
            ),
            body: Padding(
              padding: const EdgeInsets.all(16.0),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  _buildProtocolSelector(context, state),
                  const SizedBox(height: 16),
                  _buildHostField(context),
                  const SizedBox(height: 16),
                  _buildPortField(context),
                  const SizedBox(height: 16),
                  _buildPathField(context),
                  const SizedBox(height: 24),
                  _buildPreview(state),
                ],
              ),
            ),
          );
        },
      ),
    );
  }

  Widget _buildProtocolSelector(BuildContext context, AddApiState state) {
    return SegmentedButton<String>(
      segments: const [
        ButtonSegment<String>(value: 'http', label: Text('HTTP')),
        ButtonSegment<String>(value: 'https', label: Text('HTTPS')),
      ],
      selected: {state.protocol},
      onSelectionChanged: (Set<String> newSelection) {
        context.read<AddApiBloc>().add(
              ProtocolChanged(newSelection.first),
            );
      },
    );
  }

  Widget _buildHostField(BuildContext context) {
    return TextFormField(
      controller: _hostController,
      decoration: const InputDecoration(
        labelText: 'Host',
        hintText: 'example.com',
        border: OutlineInputBorder(),
        prefixIcon: Icon(Icons.dns),
      ),
      onChanged: (value) => context.read<AddApiBloc>().add(
            HostChanged(value),
          ),
    );
  }

  Widget _buildPortField(BuildContext context) {
    return TextFormField(
      controller: _portController,
      decoration: const InputDecoration(
        labelText: 'Port',
        hintText: '5000',
        border: OutlineInputBorder(),
        prefixIcon: Icon(Icons.numbers),
      ),
      keyboardType: TextInputType.number,
      inputFormatters: [FilteringTextInputFormatter.digitsOnly],
      onChanged: (value) => context.read<AddApiBloc>().add(
            PortChanged(int.tryParse(value) ?? 0),
          ),
    );
  }

  Widget _buildPathField(BuildContext context) {
    return TextFormField(
      controller: _urlPathController,
      decoration: const InputDecoration(
        labelText: 'API Path',
        hintText: 'api/v1',
        border: OutlineInputBorder(),
        prefixIcon: Icon(Icons.link),
      ),
      onChanged: (value) => context.read<AddApiBloc>().add(
            PathChanged(value),
          ),
    );
  }

  Widget _buildPreview(AddApiState state) {
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16.0),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const Text(
              'Preview:',
              style: TextStyle(
                fontSize: 16,
                fontWeight: FontWeight.bold,
              ),
            ),
            const SizedBox(height: 8),
            Text(
              state.fullUrl,
              style: const TextStyle(
                fontSize: 16,
                fontFamily: 'monospace',
              ),
            ),
          ],
        ),
      ),
    );
  }
}
