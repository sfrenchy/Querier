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
      child: Container(
        constraints: const BoxConstraints(
          minHeight: 100,
          maxHeight: 400,
        ),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            CardHeader(
              title: card.titles[Localizations.localeOf(context).languageCode] ??
                  card.titles['en'] ??
                  '',
              onEdit: onEdit,
              onDelete: onDelete,
            ),
            Flexible(
              child: Container(
                padding: const EdgeInsets.all(8.0),
                child: buildCardContent(context),
              ),
            ),
          ],
        ),
      ),
    );
  }
}
