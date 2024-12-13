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
    return SizedBox(
      width: width ?? 300,
      height: height ?? 200,
      child: Container(
        constraints: const BoxConstraints(
          minWidth: 200,
          minHeight: 100,
        ),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                Padding(
                  padding: const EdgeInsets.all(8.0),
                  child: Text(
                    title,
                    style: const TextStyle(fontWeight: FontWeight.bold),
                  ),
                ),
                if (isEditable)
                  IconButton(
                    icon: const Icon(Icons.settings),
                    onPressed: () => _showConfigDialog(context),
                    tooltip: 'Configure',
                  ),
              ],
            ),
            Padding(
              padding: const EdgeInsets.symmetric(horizontal: 16.0),
              child: Container(
                height: 1,
                color: Colors.white,
                width: double.infinity,
                margin: const EdgeInsets.only(bottom: 8.0),
              ),
            ),
            Expanded(
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
