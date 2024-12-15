import 'package:querier/models/cards/base_card.dart';
import 'package:querier/models/card_type.dart';

class PlaceholderCard extends BaseCard {
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

  factory PlaceholderCard.fromJson(Map<String, dynamic> json) {
    return PlaceholderCard(
      id: json['Id'],
      titles: Map<String, String>.from(json['Titles']),
      order: json['Order'],
      height: json['Height'],
      width: json['Width'],
      useAvailableWidth: json['UseAvailableWidth'] ?? true,
      useAvailableHeight: json['UseAvailableHeight'] ?? true,
      backgroundColor: json['BackgroundColor'],
      textColor: json['TextColor'],
    );
  }

  @override
  Map<String, dynamic> get specificConfiguration => {};
}
