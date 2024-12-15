import 'package:flutter/material.dart';
import 'package:querier/models/cards/placeholder_card.dart';
import 'package:querier/widgets/cards/base_card_widget.dart';

class PlaceholderCardWidget extends BaseCardWidget {
  const PlaceholderCardWidget({
    super.key,
    required super.card,
    super.onEdit,
    super.onDelete,
    super.dragHandle,
  });

  @override
  Widget buildCardContent(BuildContext context) {
    final placeholderCard = card as PlaceholderCard;
    return Center(
      child: Text(
        placeholderCard.getLocalizedLabel(
          Localizations.localeOf(context).languageCode,
        ),
        style: TextStyle(
          color: placeholderCard.textColor != null
              ? Color(placeholderCard.textColor!)
              : null,
        ),
      ),
    );
  }
}
