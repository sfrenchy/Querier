import 'package:flutter/material.dart';
import 'package:querier/models/dynamic_card.dart';
import 'package:querier/widgets/cards/base_card_widget.dart';

class FLLineChartCardWidget extends BaseCardWidget {
  const FLLineChartCardWidget({
    super.key,
    required DynamicCard card,
    VoidCallback? onEdit,
    VoidCallback? onDelete,
    Widget? dragHandle,
    bool isEditing = false,
    super.maxRowHeight,
  }) : super(
          card: card,
          onEdit: onEdit,
          onDelete: onDelete,
          dragHandle: dragHandle,
          isEditing: isEditing,
        );

  @override
  Widget buildCardContent(BuildContext context) {
    return const Center(
      child: Text('Line Chart Placeholder'),
    );
  }

  @override
  Widget buildFooter(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(8.0),
      child: const Text('Chart Footer'),
    );
  }
}
