import 'package:flutter/material.dart';
import 'package:querier/models/dynamic_row.dart';
import 'package:flutter_gen/gen_l10n/app_localizations.dart';

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

    return DragTarget<DynamicRow>(
      onWillAccept: (data) => data != null && data.id != row.id,
      onAccept: (data) {
        final oldIndex = data.order - 1;
        final newIndex = row.order - 1;
        onReorder(oldIndex, newIndex);
      },
      builder: (context, candidateData, rejectedData) {
        return Container(
          decoration: BoxDecoration(
            border: candidateData.isNotEmpty
                ? Border.all(
                    color: Theme.of(context).primaryColor,
                    width: 2,
                  )
                : null,
            borderRadius: BorderRadius.circular(8),
          ),
          child: Draggable<DynamicRow>(
            data: row,
            feedback: Material(
              elevation: 4,
              child: Container(
                width: MediaQuery.of(context).size.width * 0.8,
                padding: const EdgeInsets.all(16),
                decoration: BoxDecoration(
                  color: Theme.of(context).cardColor.withOpacity(0.9),
                  borderRadius: BorderRadius.circular(8),
                ),
                child: Row(
                  children: [
                    const Icon(Icons.drag_indicator),
                    const SizedBox(width: 16),
                    Text(l10n.row(row.order)),
                  ],
                ),
              ),
            ),
            child: Card(
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
                  Container(
                    padding: const EdgeInsets.all(8),
                    decoration: BoxDecoration(
                      border: Border.all(color: Colors.grey.shade300),
                      borderRadius: BorderRadius.circular(4),
                    ),
                    child: Row(
                      mainAxisAlignment: row.alignment,
                      crossAxisAlignment: row.crossAlignment,
                      children: [
                        Text(l10n.dropCardsHere),
                      ],
                    ),
                  ),
                ],
              ),
            ),
          ),
        );
      },
    );
  }
}
