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
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:responsive_grid/responsive_grid.dart';

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

  void _showCardConfig(BuildContext context, DynamicCard card) {
    final bloc = context.read<DynamicPageLayoutBloc>();
    
    Navigator.push(
      context,
      MaterialPageRoute(
        builder: (context) => BlocProvider.value(
          value: bloc,
          child: CardConfigScreen(
            card: card,
            onSave: (updatedCard) {
              bloc.add(UpdateCard(row.id, updatedCard));
              Navigator.pop(context);
            },
          ),
        ),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    // Calculer l'espace disponible
    int usedWidth = row.cards.fold(0, (sum, card) => sum + card.gridWidth);
    int availableWidth = 12 - usedWidth;

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
      child: Container(
        padding: const EdgeInsets.all(8),
        decoration: BoxDecoration(
          border: Border.all(
            color: Colors.grey.shade300,
            width: 1,
          ),
          borderRadius: BorderRadius.circular(8),
        ),
        child: ResponsiveGridRow(
          children: [
            // Afficher les cartes existantes
            ...row.cards.map((card) => 
              ResponsiveGridCol(
                xs: 12,
                sm: 6,
                md: card.gridWidth,
                lg: card.gridWidth,
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
            // Zone de drop si espace disponible
            if (availableWidth > 0)
              ResponsiveGridCol(
                xs: 12,
                sm: 6,
                md: availableWidth,
                lg: availableWidth,
                child: DragTarget<String>(
                  onWillAccept: (data) => data == 'placeholder',
                  onAccept: (data) {
                    context.read<DynamicPageLayoutBloc>().add(
                      AddCardToRow(
                        row.id, 
                        data,
                        gridWidth: availableWidth,  // Passer la largeur disponible
                      ),
                    );
                  },
                  builder: (context, candidateData, rejectedData) {
                    return Container(
                      height: 200,
                      decoration: BoxDecoration(
                        border: Border.all(
                          color: candidateData.isNotEmpty 
                            ? Theme.of(context).primaryColor 
                            : Colors.grey.shade300,
                        ),
                        borderRadius: BorderRadius.circular(8),
                      ),
                      child: Center(
                        child: Text(
                          AppLocalizations.of(context)!.dropCardsHere,
                          style: TextStyle(color: Colors.grey.shade600),
                        ),
                      ),
                    );
                  },
                ),
              ),
          ],
        ),
      ),
    );
  }
}
