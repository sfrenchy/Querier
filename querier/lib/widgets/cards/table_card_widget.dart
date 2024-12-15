import 'package:flutter/material.dart';
import 'package:querier/models/cards/table_card.dart';
import 'package:querier/widgets/cards/base_card_widget.dart';

class TableCardWidget extends BaseCardWidget {
  const TableCardWidget({
    super.key,
    required TableCard super.card,
    super.onEdit,
    super.onDelete,
    super.dragHandle,
  });

  @override
  Widget buildCardContent(BuildContext context) {
    final tableCard = card as TableCard;
    
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