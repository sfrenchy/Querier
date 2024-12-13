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
  final Function(int, int) onReorder;
  final Function(DynamicCard) onAcceptCard;
  final Function(int, int, int) onReorderCards;
  final bool isEditing;

  const DraggableRow({
    Key? key,
    required this.row,
    required this.onEdit,
    required this.onDelete,
    required this.onReorder,
    required this.onAcceptCard,
    required this.onReorderCards,
    this.isEditing = false,
  }) : super(key: key);

  Future<void> _confirmDeleteCard(
      BuildContext context, DynamicCard card) async {
    final l10n = AppLocalizations.of(context)!;
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (dialogContext) => AlertDialog(
        title: Text(l10n.deleteCard),
        content: Text(l10n.deleteCardConfirmation),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(dialogContext, false),
            child: Text(l10n.cancel),
          ),
          TextButton(
            onPressed: () => Navigator.pop(dialogContext, true),
            child: Text(l10n.delete),
          ),
        ],
      ),
    );

    if (confirmed == true && context.mounted) {
      context.read<DynamicPageLayoutBloc>().add(
            DeleteCard(row.id, card.id),
          );
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
    // Calculer la somme des gridWidth
    final totalGridWidth =
        row.cards.fold<int>(0, (sum, card) => sum + card.gridWidth);

    return Container(
      padding: const EdgeInsets.all(8),
      decoration: isEditing
          ? BoxDecoration(
              border: Border.all(
                color: Colors.grey.shade300,
                width: 1,
              ),
              borderRadius: BorderRadius.circular(8),
            )
          : null,
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          if (isEditing)
            Column(
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
          const SizedBox(width: 8), // Espacement
          // Contenu existant
          Expanded(
            child: ResponsiveGridRow(
              children: [
                // Afficher les cartes existantes
                ...row.cards.map(
                  (card) => ResponsiveGridCol(
                    xs: 12,
                    sm: 6,
                    md: card.gridWidth,
                    lg: card.gridWidth,
                    child: CardSelector(
                      card: card,
                      onEdit: isEditing
                          ? () => _showCardConfig(context, card)
                          : null,
                      onDelete: isEditing
                          ? () => _confirmDeleteCard(context, card)
                          : null,
                      dragHandle: isEditing
                          ? MouseRegion(
                              cursor: SystemMouseCursors.grab,
                              child: const Icon(Icons.drag_handle),
                            )
                          : null,
                      isEditing: isEditing,
                    ),
                  ),
                ),
                // Zone de drop uniquement si l'espace est disponible
                if (totalGridWidth < 12 && isEditing)
                  ResponsiveGridCol(
                    xs: 12,
                    sm: 6,
                    md: 12 - totalGridWidth,
                    lg: 12 - totalGridWidth,
                    child: DragTarget<DynamicCard>(
                      onWillAccept: (data) {
                        print('Card onWillAccept: ${data?.runtimeType}');
                        return data != null;
                      },
                      onAccept: (data) {
                        print('Card onAccept: ${data.runtimeType}');
                        onAcceptCard(data);
                      },
                      builder: (context, candidateData, rejectedData) {
                        final bool isHovering = candidateData.isNotEmpty;
                        return AnimatedContainer(
                          duration: const Duration(milliseconds: 200),
                          height: isHovering ? 80 : 40,
                          margin: const EdgeInsets.all(8),
                          decoration: BoxDecoration(
                            color: Theme.of(context).colorScheme.surface,
                            border: Border.all(
                              color: isHovering
                                  ? Theme.of(context).colorScheme.primary
                                  : Theme.of(context)
                                      .colorScheme
                                      .outline
                                      .withOpacity(0.5),
                              width: isHovering ? 2 : 1,
                            ),
                            borderRadius: BorderRadius.circular(8),
                          ),
                          child: Center(
                            child: Text(
                              'Drop a card here',
                              style: TextStyle(
                                color: Theme.of(context).colorScheme.onSurface,
                                fontWeight: isHovering
                                    ? FontWeight.w500
                                    : FontWeight.normal,
                              ),
                            ),
                          ),
                        );
                      },
                    ),
                  ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}
