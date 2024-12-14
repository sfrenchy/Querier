import 'package:flutter/material.dart';
import 'package:querier/models/card_type.dart';
import 'package:querier/models/dynamic_card.dart';
import 'package:querier/pages/settings/page_layout/bloc/page_layout_bloc.dart';
import 'package:querier/widgets/cards/placeholder_card.dart';
import 'package:querier/widgets/cards/table_card.dart';
import 'package:provider/provider.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

class DynamicCardWidget extends StatelessWidget {
  final String title;
  final double? height;
  final double? width;
  final bool isResizable;
  final bool isCollapsible;
  final Widget child;
  final bool isEditable;
  final VoidCallback? onConfigurePressed;

  const DynamicCardWidget({
    super.key,
    required this.title,
    this.height,
    this.width,
    this.isResizable = false,
    this.isCollapsible = true,
    required this.child,
    this.isEditable = false,
    this.onConfigurePressed,
  });

  factory DynamicCardWidget.fromModel({
    required DynamicCard model,
    required bool isEditable,
    required BuildContext context,
  }) {
    final pageLayoutBloc = context.read<PageLayoutBloc>();
    Widget cardContent;
    final cardWidth = model.useAvailableWidth ? double.infinity : model.width;

    switch (model.type) {
      case CardType.Table:
        print('Card type: ${model.type}'); // Debug
        cardContent = TableCard(
          title: model
              .getLocalizedTitle(Localizations.localeOf(context).languageCode),
          cardId: model.id,
          height: model.height,
          width: cardWidth,
          isResizable: model.isResizable,
          isCollapsible: model.isCollapsible,
          isEditable: isEditable,
          pageLayoutBloc: pageLayoutBloc,
          useAvailableWidth: model.useAvailableWidth,
          useAvailableHeight: model.useAvailableHeight,
        );
        break;
      default:
        print('Default case - Card type: ${model.type}'); // Debug
        cardContent = PlaceholderCard(
          title: model
              .getLocalizedTitle(Localizations.localeOf(context).languageCode),
          cardId: model.id,
          height: model.height,
          width: cardWidth,
          isResizable: model.isResizable,
          isCollapsible: model.isCollapsible,
          placeholderText:
              model.configuration?['placeholderText'] ?? 'Placeholder Card',
          isEditable: isEditable,
          pageLayoutBloc: pageLayoutBloc,
          useAvailableWidth: model.useAvailableWidth,
          useAvailableHeight: model.useAvailableHeight,
        );
    }

    return DynamicCardWidget(
      title:
          model.getLocalizedTitle(Localizations.localeOf(context).languageCode),
      height: model.height,
      width: cardWidth,
      isResizable: model.isResizable,
      isCollapsible: model.isCollapsible,
      isEditable: isEditable,
      child: cardContent,
    );
  }

  @override
  Widget build(BuildContext context) {
    return Container(
      constraints: BoxConstraints(
        minWidth: width != null ? width! : 200.0,
        minHeight: 100,
        maxWidth: width ?? double.infinity,
      ),
      child: child,
    );
  }
}
