import 'package:flutter/material.dart';
import 'package:querier/models/dynamic_card.dart';
import 'package:querier/models/cards/placeholder_card.dart';
import 'package:querier/widgets/cards/placeholder_card_widget.dart';

class CardSelector extends StatelessWidget {
  final DynamicCard card;
  final VoidCallback onEdit;
  final VoidCallback onDelete;
  final Widget? dragHandle;

  const CardSelector({
    Key? key,
    required this.card,
    required this.onEdit,
    required this.onDelete,
    this.dragHandle,
  }) : super(key: key);

  @override
  Widget build(BuildContext context) {
    switch (card.type) {
      case 'Placeholder':
        final placeholderCard = PlaceholderCard(
          id: card.id,
          titles: card.titles,
          order: card.order,
          height: card.height,
          width: card.width,
          useAvailableWidth: card.useAvailableWidth,
          useAvailableHeight: card.useAvailableHeight,
          backgroundColor: card.backgroundColor,
          textColor: card.textColor,
          configuration: card.configuration,
        );
        return PlaceholderCardWidget(
          card: placeholderCard,
          onEdit: onEdit,
          onDelete: onDelete,
          dragHandle: dragHandle,
        );
      default:
        return Card(
          child: Center(
            child: Text('Unknown card type: ${card.type}'),
          ),
        );
    }
  }
} 