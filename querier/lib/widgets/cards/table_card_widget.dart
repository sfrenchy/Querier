import 'package:flutter/material.dart';
import 'package:querier/models/cards/table_card.dart';
import 'package:provider/provider.dart';
import 'package:querier/api/api_client.dart';

class TableCardWidget extends StatefulWidget {
  final TableCard card;
  final VoidCallback? onEdit;
  final VoidCallback? onDelete;
  final Widget? dragHandle;

  const TableCardWidget({
    super.key, 
    required this.card,
    this.onEdit,
    this.onDelete,
    this.dragHandle,
  });

  @override
  State<TableCardWidget> createState() => _TableCardWidgetState();
}

class _TableCardWidgetState extends State<TableCardWidget> {
  List<Map<String, dynamic>> _data = [];
  bool _isLoading = false;

  @override
  void initState() {
    super.initState();
    _loadData();
  }

  Future<void> _loadData() async {
    if (widget.card.configuration['context'] == null || 
        widget.card.configuration['entity'] == null) {
      return;
    }

    setState(() => _isLoading = true);

    try {
      final apiClient = context.read<ApiClient>();
      final (data, _) = await apiClient.getEntityData(
        widget.card.configuration['context'],
        widget.card.configuration['entity'],
      );
      setState(() {
        _data = data;
        _isLoading = false;
      });
    } catch (e) {
      setState(() => _isLoading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    if (_isLoading) {
      return const Center(child: CircularProgressIndicator());
    }

    if (_data.isEmpty) {
      return const Center(child: Text('No data available'));
    }

    // Créer les colonnes à partir des clés de la première ligne
    final columns = _data.first.keys.map((key) => 
      DataColumn(label: Text(key))
    ).toList();

    // Créer les lignes à partir des données
    final rows = _data.map((row) => 
      DataRow(
        cells: row.values.map((value) => 
          DataCell(Text(value?.toString() ?? ''))
        ).toList(),
      )
    ).toList();

    return SingleChildScrollView(
      scrollDirection: Axis.horizontal,
      child: DataTable(
        columns: columns,
        rows: rows,
      ),
    );
  }
} 