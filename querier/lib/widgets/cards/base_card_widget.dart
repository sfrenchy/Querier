import 'package:flutter/material.dart';
import 'package:querier/models/cards/base_card.dart';
import 'package:querier/widgets/cards/card_header.dart';

abstract class BaseCardWidget extends StatelessWidget {
  final BaseCard card;
  final bool isEditing;
  final VoidCallback? onEdit;
  final VoidCallback? onDelete;
  final ValueChanged<Size>? onResize;

  const BaseCardWidget({
    super.key,
    required this.card,
    this.isEditing = false,
    this.onEdit,
    this.onDelete,
    this.onResize,
  });

  Widget buildCardContent(BuildContext context);

  @override
  Widget build(BuildContext context) {
    return Card(
      color: card.backgroundColor != null ? Color(card.backgroundColor!) : null,
      child: Column(
        children: [
          CardHeader(
            title: card.titles[Localizations.localeOf(context).languageCode] ??
                card.titles['en'] ??
                '',
            onEdit: onEdit,
            onDelete: onDelete,
          ),
          Container(
            width:
                card.width ?? (card.useAvailableWidth ? double.infinity : null),
            height: card.height ??
                (card.useAvailableHeight ? double.infinity : null),
            child: buildCardContent(context),
          ),
        ],
      ),
    );
  }
}
