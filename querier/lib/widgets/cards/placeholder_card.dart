import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:querier/models/card_type.dart';
import 'package:querier/pages/settings/page_layout/bloc/page_layout_bloc.dart';
import 'package:querier/pages/settings/page_layout/bloc/page_layout_event.dart';
import 'package:querier/widgets/cards/base_card.dart';
import 'package:querier/widgets/cards/common_card_config_form.dart';
import 'package:querier/pages/settings/card_config/card_config_screen.dart';
import 'package:querier/models/dynamic_card.dart';
import 'package:querier/widgets/cards/base_card_layout.dart';

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
  final bool useAvailableWidth;
  final bool useAvailableHeight;

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
    this.useAvailableWidth = false,
    this.useAvailableHeight = false,
  });

  @override
  Widget build(BuildContext context) {
    return BaseCard(
      title: title,
      cardId: cardId,
      isEditable: isEditable,
      pageLayoutBloc: pageLayoutBloc,
      onConfigurePressed: () {
        print("Opening config for PlaceholderCard");
        showDialog(
          context: context,
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
                useAvailableWidth: useAvailableWidth,
                useAvailableHeight: useAvailableHeight,
                configuration: {'placeholderText': placeholderText},
              ),
              cardType: 'placeholder',
            ),
          ),
        );
      },
      child: BaseCardLayout(
        useAvailableWidth: useAvailableWidth,
        useAvailableHeight: useAvailableHeight,
        width: width,
        height: height,
        child: Center(
          child: Text(placeholderText),
        ),
      ),
    );
  }
}
