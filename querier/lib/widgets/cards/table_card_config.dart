import 'package:flutter/material.dart';
import 'package:flutter_gen/gen_l10n/app_localizations.dart';
import 'package:querier/api/api_client.dart';
import 'package:querier/models/cards/table_card.dart';
import 'package:provider/provider.dart';

class TableCardConfig extends StatefulWidget {
  final TableCard card;
  final ValueChanged<Map<String, dynamic>> onConfigurationChanged;

  const TableCardConfig({
    Key? key,
    required this.card,
    required this.onConfigurationChanged,
  }) : super(key: key);

  @override
  State<TableCardConfig> createState() => _TableCardConfigState();
}

class _TableCardConfigState extends State<TableCardConfig> {
  List<String> _contexts = [];
  String? _selectedContext;
  bool _isLoading = true;

  @override
  void initState() {
    super.initState();
    _loadContexts();
  }

  Future<void> _loadContexts() async {
    try {
      final apiClient = context.read<ApiClient>();
      final contexts = await apiClient.getEntityContexts();
      setState(() {
        _contexts = contexts;
        _isLoading = false;
        _selectedContext = widget.card.configuration['context'] as String?;
      });
    } catch (e) {
      setState(() => _isLoading = false);
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('Error loading contexts: $e')),
        );
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context)!;

    if (_isLoading) {
      return const Center(child: CircularProgressIndicator());
    }

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        ListTile(
          title: Text(l10n.dataContext),
          subtitle: DropdownButton<String>(
            value: _selectedContext,
            isExpanded: true,
            hint: Text(l10n.selectDataContext),
            items: _contexts.map((context) => 
              DropdownMenuItem(
                value: context,
                child: Text(context),
              ),
            ).toList(),
            onChanged: (value) {
              setState(() => _selectedContext = value);
              if (value != null) {
                final newConfig = Map<String, dynamic>.from(widget.card.configuration);
                newConfig['context'] = value;
                widget.onConfigurationChanged(newConfig);
              }
            },
          ),
        ),
      ],
    );
  }
} 