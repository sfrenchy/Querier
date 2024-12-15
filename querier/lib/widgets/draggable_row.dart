import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:querier/models/dynamic_row.dart';
import 'package:flutter_gen/gen_l10n/app_localizations.dart';
import 'package:querier/pages/settings/menu/pages/bloc/dynamic_page_layout_event.dart';
import 'package:querier/pages/settings/menu/pages/bloc/dynamic_page_layout_bloc.dart';

class DraggableRow extends StatelessWidget {
  final DynamicRow row;
  final VoidCallback onEdit;
  final VoidCallback onDelete;
  final Function(int oldIndex, int newIndex) onReorder;

  const DraggableRow({
    super.key,
    required this.row,
    required this.onEdit,
    required this.onDelete,
    required this.onReorder,
  });

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context)!;

    return Card(
      child: Column(
        children: [
          ListTile(
            leading: MouseRegion(
              cursor: SystemMouseCursors.grab,
              child: const Icon(Icons.drag_indicator),
            ),
            title: Text(l10n.row(row.order)),
            trailing: Row(
              mainAxisSize: MainAxisSize.min,
              children: [
                IconButton(
                  icon: const Icon(Icons.edit),
                  onPressed: onEdit,
                ),
                IconButton(
                  icon: const Icon(Icons.delete),
                  onPressed: onDelete,
                ),
              ],
            ),
          ),
          Padding(
            padding: const EdgeInsets.all(8.0),
            child: DragTarget<Object>(
              onWillAccept: (data) => data is String && data == 'placeholder',
              onAccept: (data) {
                if (data is String && data == 'placeholder') {
                  context
                      .read<DynamicPageLayoutBloc>()
                      .add(AddCard(row.id, data));
                }
              },
              builder: (context, candidateData, rejectedData) {
                return Container(
                  padding: const EdgeInsets.all(16),
                  decoration: BoxDecoration(
                    border: Border.all(
                      color: candidateData.isNotEmpty
                          ? Theme.of(context).primaryColor
                          : Colors.grey.shade300,
                      width: candidateData.isNotEmpty ? 2 : 1,
                    ),
                    borderRadius: BorderRadius.circular(8),
                    color: candidateData.isNotEmpty
                        ? Theme.of(context).primaryColor.withOpacity(0.1)
                        : null,
                  ),
                  child: Row(
                    mainAxisAlignment: row.alignment,
                    crossAxisAlignment: row.crossAlignment,
                    children: [
                      Expanded(
                        child: Center(
                          child: Text(
                            candidateData.isNotEmpty
                                ? l10n.dropCardHere
                                : l10n.dropCardsHere,
                            style: TextStyle(
                              color: candidateData.isNotEmpty
                                  ? Theme.of(context).primaryColor
                                  : Colors.grey,
                            ),
                          ),
                        ),
                      ),
                    ],
                  ),
                );
              },
            ),
          ),
        ],
      ),
    );
  }
}
