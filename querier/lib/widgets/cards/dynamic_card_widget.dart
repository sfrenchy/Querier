import 'package:flutter/material.dart';
import 'package:querier/models/dynamic_card.dart';
import 'package:querier/widgets/cards/placeholder_card.dart';

class DynamicCardWidget extends StatelessWidget {
  final String title;
  final double? height;
  final double? width;
  final bool isResizable;
  final bool isCollapsible;
  final Widget child;

  const DynamicCardWidget({
    super.key,
    required this.title,
    this.height,
    this.width,
    this.isResizable = false,
    this.isCollapsible = true,
    required this.child,
  });

  factory DynamicCardWidget.fromModel(DynamicCard model, BuildContext context) {
    Widget cardContent;

    switch (model.type.toLowerCase()) {
      default:
        cardContent = PlaceholderCard(
          title: model
              .getLocalizedTitle(Localizations.localeOf(context).languageCode),
          height: model.height,
          width: model.width,
          isResizable: model.isResizable,
          isCollapsible: model.isCollapsible,
        );
    }

    return DynamicCardWidget(
      title:
          model.getLocalizedTitle(Localizations.localeOf(context).languageCode),
      height: model.height,
      width: model.width,
      isResizable: model.isResizable,
      isCollapsible: model.isCollapsible,
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
