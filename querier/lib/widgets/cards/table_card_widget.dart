import 'package:flutter/material.dart';
import 'package:querier/models/cards/table_card.dart';
import 'package:querier/widgets/cards/base_card_widget.dart';

class TableCardWidget extends BaseCardWidget {
  TableCardWidget({
    super.key,
    required TableCard super.card,
    super.onEdit,
    super.onDelete,
    super.dragHandle,
  }) {
    print('TableCardWidget constructor: card = ${card.toJson()}'); // Debug
    print('TableCardWidget constructor: headerBackgroundColor = ${card.headerBackgroundColor}'); // Debug
  }

  @override
  Widget? buildHeader(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(8.0),
      child: Row(
        children: [
          IconButton(
            icon: const Icon(Icons.filter_list),
            onPressed: () {
              // Action de filtrage
            },
          ),
          IconButton(
            icon: const Icon(Icons.sort),
            onPressed: () {
              // Action de tri
            },
          ),
        ],
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
          Text('1-10 of 15'),
          IconButton(
            icon: const Icon(Icons.chevron_left),
            onPressed: () {
              // Page précédente
            },
          ),
          IconButton(
            icon: const Icon(Icons.chevron_right),
            onPressed: () {
              // Page suivante
            },
          ),
        ],
      ),
    );
  }

  @override
  Widget buildCardContent(BuildContext context) {
    final tableCard = card as TableCard;
    print('TableCardWidget.buildCardContent: headerBackgroundColor = ${tableCard.headerBackgroundColor}'); // Debug
    
    return LayoutBuilder(
      builder: (context, constraints) {
        return SingleChildScrollView(
          child: SingleChildScrollView(
            scrollDirection: Axis.horizontal,
            child: ConstrainedBox(
              constraints: BoxConstraints(
                minWidth: constraints.maxWidth,
              ),
              child: Theme(
                data: Theme.of(context).copyWith(
                  dataTableTheme: DataTableThemeData(
                    columnSpacing: 24.0,
                    horizontalMargin: 24.0,
                    headingTextStyle: Theme.of(context).textTheme.titleSmall,
                    dataRowMinHeight: 48.0,
                    dataRowMaxHeight: 48.0,
                    dividerThickness: 1.0,
                  ),
                ),
                child: DataTable(
                  columns: tableCard.columns.map((col) => 
                    DataColumn(
                      label: Text(
                        col['label'][Localizations.localeOf(context).languageCode] ?? 
                        col['label']['en'] ?? 
                        col['key'],
                      ),
                    ),
                  ).toList(),
                  rows: tableCard.data.map((row) => 
                    DataRow(
                      cells: tableCard.columns.map((col) => 
                        DataCell(
                          Text(row[col['key']]?.toString() ?? ''),
                        ),
                      ).toList(),
                    ),
                  ).toList(),
                ),
              ),
            ),
          ),
        );
      },
    );
  }
} 