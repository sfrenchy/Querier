import 'package:querier/models/cards/base_card.dart';

class DynamicCard extends BaseCard {
  final Map<String, dynamic> configuration;

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
      height: json['Height'],
      width: json['Width'],
      useAvailableWidth: json['UseAvailableWidth'] ?? true,
      useAvailableHeight: json['UseAvailableHeight'] ?? true,
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
    double? height,
    double? width,
    bool? useAvailableWidth,
    bool? useAvailableHeight,
    int? backgroundColor,
    int? textColor,
    Map<String, dynamic>? configuration,
  }) {
    return DynamicCard(
      id: id ?? this.id,
      titles: titles ?? this.titles,
      order: order ?? this.order,
      type: type ?? this.type,
      height: height ?? this.height,
      width: width ?? this.width,
      useAvailableWidth: useAvailableWidth ?? this.useAvailableWidth,
      useAvailableHeight: useAvailableHeight ?? this.useAvailableHeight,
      backgroundColor: backgroundColor ?? this.backgroundColor,
      textColor: textColor ?? this.textColor,
      configuration: configuration ?? this.configuration,
    );
  }
}
