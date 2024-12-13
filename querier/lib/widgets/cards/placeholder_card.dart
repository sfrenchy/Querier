import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:querier/models/card_type.dart';
import 'package:querier/pages/settings/page_layout/bloc/page_layout_bloc.dart';
import 'package:querier/pages/settings/page_layout/bloc/page_layout_event.dart';
import 'package:querier/widgets/cards/common_card_config_form.dart';
import 'package:querier/pages/settings/card_config/card_config_screen.dart';
import 'package:querier/models/dynamic_card.dart';

class PlaceholderCard extends StatelessWidget {
  final String title;
  final int cardId;
  final double? height;
  final double? width;
  final bool isResizable;
  final bool isCollapsible;
  final String placeholderText;
  final bool isEditable;
  final PageLayoutBloc pageLayoutBloc;

  const PlaceholderCard({
    super.key,
    required this.title,
    required this.cardId,
    this.height,
    this.width,
    this.isResizable = false,
    this.isCollapsible = true,
    this.placeholderText = 'Placeholder Card',
    this.isEditable = false,
    required this.pageLayoutBloc,
  });

  @override
  Widget build(BuildContext context) {
    return Card(
      child: Container(
        height: height,
        width: width,
        padding: const EdgeInsets.all(16.0),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                Text(
                  title,
                  style: Theme.of(context).textTheme.titleLarge,
                ),
                if (isEditable)
                  Row(
                    children: [
                      IconButton(
                        icon: const Icon(Icons.settings),
                        onPressed: () {
                          _showConfigDialog(context);
                        },
                      ),
                      if (isCollapsible)
                        IconButton(
                          icon: const Icon(Icons.more_vert),
                          onPressed: () {
                            // Menu pour éditer/supprimer la carte
                          },
                        ),
                    ],
                  ),
              ],
            ),
            Flexible(
              child: Center(
                child: Text(placeholderText),
              ),
            ),
          ],
        ),
      ),
    );
  }

  void _showConfigDialog(BuildContext context) {
    Navigator.push(
      context,
      MaterialPageRoute(
        builder: (context) => BlocProvider.value(
          value: pageLayoutBloc,
          child: CardConfigScreen(
            card: DynamicCard(
              id: cardId,
              titles: {'en': title, 'fr': title},
              type: CardType.Placeholder,
              order: 0,
              isResizable: isResizable,
              isCollapsible: isCollapsible,
              height: height,
              width: width,
              configuration: {'placeholderText': placeholderText},
            ),
            cardType: 'placeholder',
          ),
        ),
      ),
    );
  }
}
