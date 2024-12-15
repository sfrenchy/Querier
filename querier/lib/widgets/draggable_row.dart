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
    return ResponsiveGridRow(
      children: row.cards.map((card) => 
        ResponsiveGridCol(
          xs: 12,    // Pleine largeur sur très petit écran
          sm: 6,     // Demi largeur sur petit écran
          md: card.useAvailableWidth ? 12 : 6,  // Adaptatif selon useAvailableWidth
          lg: card.useAvailableWidth ? 12 : 4,  // Adaptatif selon useAvailableWidth
          child: Padding(
            padding: const EdgeInsets.all(8.0),
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
      ).toList(),
    );
  }
}
