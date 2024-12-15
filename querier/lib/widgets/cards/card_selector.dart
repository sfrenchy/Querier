import 'package:flutter/material.dart';
import 'package:querier/models/dynamic_card.dart';
import 'package:querier/models/cards/placeholder_card.dart';
import 'package:querier/widgets/cards/placeholder_card_config.dart';
import 'package:querier/widgets/cards/placeholder_card_widget.dart';
import 'package:querier/models/cards/table_card.dart';
import 'package:querier/widgets/cards/table_card_widget.dart';

class CardSelector extends StatelessWidget {
  final DynamicCard card;
  final VoidCallback onEdit;
  final VoidCallback onDelete;
  final Widget? dragHandle;
  final ValueChanged<Map<String, dynamic>>? onConfigurationChanged;

  const CardSelector({
    Key? key,
    required this.card,
    required this.onEdit,
    required this.onDelete,
    this.dragHandle,
    this.onConfigurationChanged,
  }) : super(key: key);

  Widget? buildConfigurationWidget() {
    switch (card.type) {
      case 'Placeholder':
        if (onConfigurationChanged != null) {
          final placeholderCard = PlaceholderCard(
            id: card.id,
            titles: card.titles,
            order: card.order,
            gridWidth: card.gridWidth,
            backgroundColor: card.backgroundColor,
            textColor: card.textColor,
            configuration: card.configuration,
          );
          return PlaceholderCardConfig(
            card: placeholderCard,
            onConfigurationChanged: onConfigurationChanged!,
          );
        }
        return null;
      case 'Table':
        if (onConfigurationChanged != null) {
          final tableCard = TableCard(
            id: card.id,
            titles: card.titles,
            order: card.order,
            gridWidth: card.gridWidth,
            backgroundColor: card.backgroundColor,
            textColor: card.textColor,
            headerBackgroundColor: card.headerBackgroundColor,
            headerTextColor: card.headerTextColor,
            configuration: card.configuration,
          );
          return null;
        }
        return null;
      default:
        return null;
    }
  }

  @override
  Widget build(BuildContext context) {
    switch (card.type) {
      case 'Placeholder':
        final placeholderCard = PlaceholderCard(
          id: card.id,
          titles: card.titles,
          order: card.order,
          gridWidth: card.gridWidth,
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
      case 'Table':
        final tableCard = TableCard(
          id: card.id,
          titles: card.titles,
          order: card.order,
          gridWidth: card.gridWidth,
          backgroundColor: card.backgroundColor,
          textColor: card.textColor,
          headerBackgroundColor: card.headerBackgroundColor,
          headerTextColor: card.headerTextColor,
          configuration: card.configuration,
        );
        return TableCardWidget(
          card: tableCard,
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