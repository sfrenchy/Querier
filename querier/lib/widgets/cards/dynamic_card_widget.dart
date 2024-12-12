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

  factory DynamicCardWidget.fromModel(DynamicCard model) {
    Widget cardContent;

    switch (model.type.toLowerCase()) {
      /*case 'table':
        cardContent = TableCard(
          title: model.title,
          configuration: model.configuration,
          height: model.height,
          width: model.width,
          isResizable: model.isResizable,
          isCollapsible: model.isCollapsible,
        );
        break;
      case 'chart':
        cardContent = ChartCard(
          title: model.title,
          configuration: model.configuration,
          height: model.height,
          width: model.width,
          isResizable: model.isResizable,
          isCollapsible: model.isCollapsible,
        );
        break;
      case 'metrics':
        cardContent = MetricsCard(
          title: model.title,
          configuration: model.configuration,
          height: model.height,
          width: model.width,
          isResizable: model.isResizable,
          isCollapsible: model.isCollapsible,
        );
        break;*/
      default:
        cardContent = PlaceholderCard(
          title: model.title,
          height: model.height,
          width: model.width,
          isResizable: model.isResizable,
          isCollapsible: model.isCollapsible,
        );
    }

    return DynamicCardWidget(
      title: model.title,
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
