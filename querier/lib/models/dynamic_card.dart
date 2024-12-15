import 'package:querier/models/cards/base_card.dart';

class DynamicCard extends BaseCard {
  const DynamicCard({
    required super.id,
    required super.titles,
    required super.order,
    required super.type,
    super.height,
    super.width,
    super.useAvailableWidth,
    super.useAvailableHeight,
    super.backgroundColor,
    super.textColor,
  });

  @override
  Map<String, dynamic> get specificConfiguration => {};

  factory DynamicCard.fromJson(Map<String, dynamic> json) {
    return DynamicCard(
      id: json['Id'],
      titles: Map<String, String>.from(json['Titles']),
      order: json['Order'],
      type: json['Type'],
      height: json['Height'],
      width: json['Width'],
      useAvailableWidth: json['UseAvailableWidth'] ?? true,
      useAvailableHeight: json['UseAvailableHeight'] ?? true,
      backgroundColor: json['BackgroundColor'],
      textColor: json['TextColor'],
    );
  }
}
