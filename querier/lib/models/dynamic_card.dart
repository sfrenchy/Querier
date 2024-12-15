import 'package:querier/models/cards/base_card.dart';

class DynamicCard extends BaseCard {
  final int gridWidth;
  final int? backgroundColor;
  final int? textColor;
  final Map<String, dynamic> configuration;

  const DynamicCard({
    required super.id,
    required super.titles,
    required super.order,
    required super.type,
    this.gridWidth = 12,
    this.backgroundColor,
    this.textColor,
    this.configuration = const {},
  });

  @override
  Map<String, dynamic> get specificConfiguration => configuration;

  factory DynamicCard.fromJson(Map<String, dynamic> json) {
    return DynamicCard(
      id: json['Id'],
      titles: Map<String, String>.from(json['Titles']),
      order: json['Order'],
      type: json['Type'],
      gridWidth: json['GridWidth'] ?? 12,
      backgroundColor: json['BackgroundColor'],
      textColor: json['TextColor'],
      configuration: Map<String, dynamic>.from(json['Configuration'] ?? {}),
    );
  }

  DynamicCard copyWith({
    int? id,
    Map<String, String>? titles,
    int? order,
    String? type,
    int? gridWidth,
    int? backgroundColor,
    int? textColor,
    Map<String, dynamic>? configuration,
  }) {
    return DynamicCard(
      id: id ?? this.id,
      titles: titles ?? this.titles,
      order: order ?? this.order,
      type: type ?? this.type,
      gridWidth: gridWidth ?? this.gridWidth,
      backgroundColor: backgroundColor ?? this.backgroundColor,
      textColor: textColor ?? this.textColor,
      configuration: configuration ?? this.configuration,
    );
  }
}
