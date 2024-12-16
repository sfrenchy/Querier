import 'package:flutter/material.dart';
import 'package:querier/models/cards/table_card.dart';
import 'package:querier/widgets/cards/base_card_widget.dart';
import 'package:querier/api/api_client.dart';
import 'package:provider/provider.dart';

class TableCardWidget extends BaseCardWidget {
  const TableCardWidget({
    super.key,
    required TableCard super.card,
    super.onEdit,
    super.onDelete,
    super.dragHandle,
  });

  Future<(List<Map<String, dynamic>>, int)> _loadData(BuildContext buildContext, TableCard card) async {
    final apiClient = buildContext.read<ApiClient>();
    final context = card.configuration['context'] as String?;
    final entity = card.configuration['entity'] as String?;
    
    if (context == null || entity == null) {
      throw Exception('Configuration incomplète: context et entity sont requis');
    }

    return await apiClient.getEntityData(context, entity);
  }

  @override
  Widget? buildHeader(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(8.0),
      child: Row(
        children: [
          IconButton(
            icon: const Icon(Icons.filter_list),
            onPressed: () {},
          ),
          IconButton(
            icon: const Icon(Icons.sort),
            onPressed: () {},
          ),
        ],
      ),
    );
  }

  @override
  Widget buildCardContent(BuildContext context) {
    final tableCard = card as TableCard;
    final ScrollController horizontalController = ScrollController();
    final ScrollController verticalController = ScrollController();

    return SizedBox(
      width: double.infinity,
      child: Scrollbar(
        controller: verticalController,
        thumbVisibility: true,
        trackVisibility: true,
        child: Scrollbar(
          controller: horizontalController,
          thumbVisibility: true,
          trackVisibility: true,
          notificationPredicate: (notif) => notif.depth == 1,
          child: FutureBuilder<(List<Map<String, dynamic>>, int)>(
            future: _loadData(context, tableCard),
            builder: (context, snapshot) {
              if (snapshot.connectionState == ConnectionState.waiting) {
                return const Center(child: CircularProgressIndicator());
              }

              if (snapshot.hasError) {
                return Center(
                  child: Text('Erreur: ${snapshot.error}'),
                );
              }

              if (!snapshot.hasData || snapshot.data!.$1.isEmpty) {
                return const Center(
                  child: Text('Aucune donnée disponible'),
                );
              }

              final (items, total) = snapshot.data!;
              final columns = tableCard.columns;

              return SingleChildScrollView(
                controller: verticalController,
                child: SingleChildScrollView(
                  controller: horizontalController,
                  scrollDirection: Axis.horizontal,
                  child: DataTable(
                    columns: columns.map((column) => DataColumn(
                      label: Text(
                        column['label']?[Localizations.localeOf(context).languageCode] ?? column['key'],
                        style: const TextStyle(fontWeight: FontWeight.bold),
                      ),
                    )).toList(),
                    rows: items.map((row) => DataRow(
                      cells: columns.map((column) => DataCell(
                        Text(row[column['key']]?.toString() ?? ''),
                      )).toList(),
                    )).toList(),
                  ),
                ),
              );
            },
          ),
        ),
      ),
    );
  }

  @override
  Widget? buildFooter(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(8.0),
      child: Row(
        mainAxisAlignment: MainAxisAlignment.end,
        children: [
          const Text('1-10 of 100'),
          IconButton(
            icon: const Icon(Icons.chevron_left),
            onPressed: () {},
          ),
          IconButton(
            icon: const Icon(Icons.chevron_right),
            onPressed: () {},
          ),
        ],
      ),
    );
  }
} 