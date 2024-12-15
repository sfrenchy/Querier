import 'package:flutter/material.dart';
import 'package:querier/models/dynamic_card.dart';
import 'package:querier/models/cards/placeholder_card.dart';
import 'package:querier/widgets/cards/placeholder_card_widget.dart';

class CardSelector extends StatelessWidget {
  final DynamicCard card;
  final VoidCallback onEdit;
  final VoidCallback onDelete;

  const CardSelector({
    Key? key,
    required this.card,
    required this.onEdit,
    required this.onDelete,
  }) : super(key: key);

  @override
  Widget build(BuildContext context) {
    // Sélectionner le widget approprié en fonction du type de carte
    switch (card.runtimeType) {
      case PlaceholderCard:
        return PlaceholderCardWidget(
          card: card as PlaceholderCard,
          onEdit: onEdit,
          onDelete: onDelete,
        );
      // Ajouter d'autres cas au fur et à mesure
      // case TableCard:
      //   return TableCardWidget(
      //     card: card as TableCard,
      //     onEdit: onEdit,
      //     onDelete: onDelete,
      //   );
      default:
        return Card(
          child: Center(
            child: Text('Unknown card type: ${card.runtimeType}'),
          ),
        );
    }
  }
} 