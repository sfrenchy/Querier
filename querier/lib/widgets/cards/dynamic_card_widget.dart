import 'package:flutter/material.dart';
import 'package:querier/models/dynamic_card.dart';
import 'package:querier/pages/settings/page_layout/bloc/page_layout_bloc.dart';
import 'package:querier/widgets/cards/placeholder_card.dart';

class DynamicCardWidget extends StatelessWidget {
  final String title;
  final double? height;
  final double? width;
  final bool isResizable;
  final bool isCollapsible;
  final Widget child;
  final bool isEditable;

  const DynamicCardWidget({
    super.key,
    required this.title,
    this.height,
    this.width,
    this.isResizable = false,
    this.isCollapsible = true,
    required this.child,
    this.isEditable = false,
  });

  factory DynamicCardWidget.fromModel(
    DynamicCard model,
    BuildContext context, {
    bool isEditable = false,
    required PageLayoutBloc pageLayoutBloc,
  }) {
    Widget cardContent;

    switch (model.type) {
      default:
        cardContent = PlaceholderCard(
          title: model
              .getLocalizedTitle(Localizations.localeOf(context).languageCode),
          cardId: model.id,
          height: model.height,
          width: model.width,
          isResizable: model.isResizable,
          isCollapsible: model.isCollapsible,
          placeholderText:
              model.configuration?['placeholderText'] ?? 'Placeholder Card',
          isEditable: isEditable,
          pageLayoutBloc: pageLayoutBloc,
        );
    }

    return DynamicCardWidget(
      title:
          model.getLocalizedTitle(Localizations.localeOf(context).languageCode),
      height: model.height,
      width: model.useAvailableWidth ? double.infinity : model.width,
      isResizable: model.isResizable,
      isCollapsible: model.isCollapsible,
      isEditable: isEditable,
      child: cardContent,
    );
  }

  @override
  Widget build(BuildContext context) {
    return Card(
      child: Container(
        height: height,
        width: width,
        constraints: const BoxConstraints(
          minWidth: 200,
          minHeight: 100,
        ),
        child: child,
      ),
    );
  }
}
