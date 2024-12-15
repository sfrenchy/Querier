import 'package:querier/models/dynamic_card.dart';

class PlaceholderCard extends DynamicCard {
  const PlaceholderCard({
    required super.id,
    required super.titles,
    required super.order,
    super.height,
    super.width,
    super.useAvailableWidth,
    super.useAvailableHeight,
    super.backgroundColor,
    super.textColor,
  }) : super(type: 'Placeholder');

  @override
  Map<String, dynamic> get specificConfiguration => {};
}
