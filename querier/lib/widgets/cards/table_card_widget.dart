import 'dart:async';
import 'dart:math';

import 'package:flutter/material.dart';
import 'package:querier/models/cards/table_card.dart';
import 'package:querier/widgets/cards/base_card_widget.dart';
import 'package:querier/api/api_client.dart';
import 'package:provider/provider.dart';
import 'package:querier/utils/data_formatter.dart';

class TableCardWidget extends BaseCardWidget {
  static const int _pageSize = 10;
  final _paginationController = StreamController<(int, int)>.broadcast();
  final _dataController = StreamController<(List<Map<String, dynamic>>, int)>.broadcast();
  
  // Cache pour stocker les données par page
  final Map<int, List<Map<String, dynamic>>> _dataCache = {};
  int? _totalItems;

  TableCardWidget({
    super.key,
    required TableCard super.card,
    super.onEdit,
    super.onDelete,
    super.dragHandle,
  });

  String _getPropertyType(String columnKey) {
    try {
      final tableCard = card as TableCard;
      final entitySchema = tableCard.configuration['entitySchema'] as Map<String, dynamic>;
      final properties = entitySchema['Properties'] as List<dynamic>;
      debugPrint('Recherche du type pour la colonne: $columnKey');
      debugPrint('Propriétés disponibles: ${properties.map((p) => p['Name'])}');
      final property = properties.firstWhere(
        (p) => p['Name'] == columnKey,
        orElse: () => {'Type': 'String'},
      );
      final type = property['Type'] as String? ?? 'String';
      debugPrint('Type trouvé: $type');
      return type;
    } catch (e) {
      debugPrint('Erreur lors de la récupération du type: $e');
    }
    return 'String';
  }

  Future<void> _loadData(BuildContext buildContext, TableCard card, {int page = 1}) async {
    // Vérifier si les données sont dans le cache
    if (_dataCache.containsKey(page)) {
      _dataController.add((_dataCache[page]!, _totalItems!));
      _paginationController.add((page, _totalItems!));
      return;
    }

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
    
    // Mettre en cache les données
    _dataCache[page] = result.$1;
    _totalItems = result.$2;
    
    _paginationController.add((page, result.$2));
    _dataController.add(result);
  }

  void clearCache() {
    _dataCache.clear();
    _totalItems = null;
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
              final columns = tableCard.columns.where((column) => 
                column['visible'] == true
              ).toList();

              return SingleChildScrollView(
                controller: verticalController,
                child: SingleChildScrollView(
                  controller: horizontalController,
                  scrollDirection: Axis.horizontal,
                  child: DataTable(
                    columns: columns.map((column) => DataColumn(
                      label: Align(
                        alignment: _getAlignment(column['alignment'] as String?),
                        child: Text(
                          column['label']?[Localizations.localeOf(context).languageCode] ?? column['key'],
                          style: const TextStyle(fontWeight: FontWeight.bold),
                        ),
                      ),
                    )).toList(),
                    rows: items.map((row) => DataRow(
                      cells: columns.map((column) => DataCell(
                        Align(
                          alignment: _getAlignment(column['alignment'] as String?),
                          child: Text(DataFormatter.format(
                            row[column['key']],
                            _getPropertyType(column['key']),
                            context,
                          )),
                        ),
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
              DropdownButton<int>(
                value: currentPage,
                isDense: true,
                items: List.generate(totalPages, (index) => index + 1)
                    .map((page) => DropdownMenuItem(
                      value: page,
                      child: Text('$page / $totalPages'),
                    )).toList(),
                onChanged: (newPage) {
                  if (newPage != null) {
                    _loadData(context, card as TableCard, page: newPage);
                  }
                },
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
    _dataCache.clear();
  }

  Alignment _getAlignment(String? alignment) {
    switch (alignment?.toLowerCase()) {
      case 'left':
        return Alignment.centerLeft;
      case 'right':
        return Alignment.centerRight;
      case 'center':
        return Alignment.center;
      default:
        return Alignment.centerLeft; // Alignement par défaut
    }
  }
} 