import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:querier/models/dynamic_card.dart';
import 'package:querier/models/dynamic_row.dart';
import 'package:querier/models/cards/placeholder_card.dart';
import 'package:querier/pages/settings/menu/pages/cards/config/card_config_screen.dart';
import 'package:querier/widgets/cards/card_selector.dart';
import 'package:querier/widgets/cards/card_header.dart';
import 'package:flutter_gen/gen_l10n/app_localizations.dart';
import 'package:querier/pages/settings/menu/pages/bloc/dynamic_page_layout_event.dart';
import 'package:querier/pages/settings/menu/pages/bloc/dynamic_page_layout_bloc.dart';

class DraggableRow extends StatelessWidget {
  final DynamicRow row;
  final VoidCallback onEdit;
  final VoidCallback onDelete;
  final Function(int oldIndex, int newIndex) onReorder;
  final Function(String cardData) onAcceptCard;
  final Function(int rowId, int oldIndex, int newIndex) onReorderCards;

  const DraggableRow({
    Key? key,
    required this.row,
    required this.onEdit,
    required this.onDelete,
    required this.onReorder,
    required this.onAcceptCard,
    required this.onReorderCards,
  }) : super(key: key);

  Future<void> _confirmDeleteCard(BuildContext context, DynamicCard card) async {
    final l10n = AppLocalizations.of(context)!;
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: Text(l10n.deleteCard),
        content: Text(l10n.deleteCardConfirmation),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context, false),
            child: Text(l10n.cancel),
          ),
          TextButton(
            onPressed: () => Navigator.pop(context, true),
            child: Text(l10n.delete),
          ),
        ],
      ),
    );

    if (confirmed == true) {
      if (context.mounted) {
        context.read<DynamicPageLayoutBloc>().add(
          DeleteCard(row.id, card.id),
        );
      }
    }
  }

  Widget _buildCard(BuildContext context, DynamicCard card) {
    return Draggable<int>(
      data: card.id,
      feedback: Material(
        elevation: 4,
        child: SizedBox(
          width: 300,
          child: CardSelector(
            card: card,
            onEdit: () => _showCardConfig(context, card),
            onDelete: () => _confirmDeleteCard(context, card),
            dragHandle: MouseRegion(
              cursor: SystemMouseCursors.grab,
              child: const Icon(Icons.drag_handle),
            ),
          ),
        ),
      ),
      child: SizedBox(
        width: 300,
        child: CardSelector(
          card: card,
          onEdit: () => _showCardConfig(context, card),
          onDelete: () => _confirmDeleteCard(context, card),
          dragHandle: MouseRegion(
            cursor: SystemMouseCursors.grab,
            child: const Icon(Icons.drag_handle),
          ),
        ),
      ),
    );
  }

  void _showCardConfig(BuildContext context, DynamicCard card) {
    Navigator.push(
      context,
      MaterialPageRoute(
        builder: (context) => CardConfigScreen(
          card: card,
          onSave: (updatedCard) {
            context.read<DynamicPageLayoutBloc>().add(
              UpdateCard(row.id, updatedCard),
            );
            Navigator.pop(context);
          },
        ),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    return Draggable<DynamicRow>(
      data: row,
      feedback: Material(
        elevation: 4,
        child: Container(
          width: 500,
          padding: const EdgeInsets.all(8),
          decoration: BoxDecoration(
            color: Theme.of(context).cardColor,
            borderRadius: BorderRadius.circular(8),
          ),
          child: Text('Row ${row.order}'),
        ),
      ),
      child: DragTarget<String>(
        onWillAccept: (data) => data == 'placeholder',
        onAccept: (data) => onAcceptCard(data),
        builder: (context, candidateData, rejectedData) {
          return Container(
            margin: const EdgeInsets.all(8.0),
            padding: const EdgeInsets.all(8.0),
            decoration: BoxDecoration(
              border: Border.all(
                color: candidateData.isNotEmpty 
                  ? Theme.of(context).primaryColor 
                  : Colors.grey.shade300,
                width: candidateData.isNotEmpty ? 2 : 1,
              ),
              borderRadius: BorderRadius.circular(8),
            ),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                Row(
                  children: [
                    MouseRegion(
                      cursor: SystemMouseCursors.grab,
                      child: const Icon(Icons.drag_handle),
                    ),
                    const Spacer(),
                    Text('Row ${row.order}'),
                    const Spacer(),
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
                Container(
                  constraints: const BoxConstraints(minHeight: 100),
                  child: SingleChildScrollView(
                    scrollDirection: Axis.horizontal,
                    child: Row(
                      children: row.cards.asMap().entries.map((entry) => 
                        _buildCard(context, entry.value)
                      ).toList(),
                    ),
                  ),
                ),
              ],
            ),
          );
        },
      ),
    );
  }
}
