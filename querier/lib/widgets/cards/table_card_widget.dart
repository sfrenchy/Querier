import 'dart:async';
import 'dart:math';

import 'package:flutter/material.dart';
import 'package:querier/models/cards/table_card.dart';
import 'package:querier/widgets/cards/base_card_widget.dart';
import 'package:querier/api/api_client.dart';
import 'package:provider/provider.dart';

class TableCardWidget extends BaseCardWidget {
  static const int _pageSize = 10;
  final _paginationController = StreamController<(int, int)>();
  final _dataController = StreamController<(List<Map<String, dynamic>>, int)>();

  TableCardWidget({
    super.key,
    required TableCard super.card,
    super.onEdit,
    super.onDelete,
    super.dragHandle,
  });

  Future<void> _loadData(BuildContext buildContext, TableCard card, {int page = 1}) async {
    final apiClient = buildContext.read<ApiClient>();
    final context = card.configuration['context'] as String?;
    final entity = card.configuration['entity'] as String?;
    
    if (context == null || entity == null) {
      throw Exception('Configuration incomplète: context et entity sont requis');
    }

    final result = await apiClient.getEntityData(
      context, 
      entity,
      pageNumber: page,
      pageSize: _pageSize,
    );
    
    _paginationController.add((page, result.$2));
    _dataController.add(result);
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

    _loadData(context, tableCard, page: 1);

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
          child: StreamBuilder<(List<Map<String, dynamic>>, int)>(
            stream: _dataController.stream,
            builder: (context, snapshot) {
              if (!snapshot.hasData) {
                return const Center(child: CircularProgressIndicator());
              }

              final (items, _) = snapshot.data!;
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
    return StreamBuilder<(int, int)>(
      stream: _paginationController.stream,
      builder: (context, snapshot) {
        if (!snapshot.hasData) return const SizedBox.shrink();
        
        final (currentPage, totalItems) = snapshot.data!;
        final startIndex = (currentPage - 1) * _pageSize + 1;
        final endIndex = min(startIndex + _pageSize - 1, totalItems);
        final totalPages = (totalItems / _pageSize).ceil();

        return Container(
          padding: const EdgeInsets.all(8.0),
          child: Row(
            mainAxisAlignment: MainAxisAlignment.end,
            children: [
              Text('$startIndex-$endIndex sur $totalItems'),
              IconButton(
                icon: const Icon(Icons.chevron_left),
                onPressed: currentPage > 1 
                  ? () => _loadData(context, card as TableCard, page: currentPage - 1)
                  : null,
              ),
              IconButton(
                icon: const Icon(Icons.chevron_right),
                onPressed: currentPage < totalPages 
                  ? () => _loadData(context, card as TableCard, page: currentPage + 1)
                  : null,
              ),
            ],
          ),
        );
      },
    );
  }

  @override
  void dispose() {
    _paginationController.close();
    _dataController.close();
  }
} 