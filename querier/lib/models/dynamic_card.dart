import 'package:querier/models/card_type.dart';

class DynamicCard {
  final int id;
  final Map<String, String> titles;
  final CardType type;
  final int order;
  final bool isResizable;
  final bool isCollapsible;
  final double? height;
  final double? width;
  final Map<String, dynamic>? configuration;
  final bool useAvailableWidth;
  final bool useAvailableHeight;
  final int? backgroundColor;
  final int? textColor;

  DynamicCard({
    required this.id,
    required this.titles,
    required this.order,
    required this.type,
    this.isResizable = false,
    this.isCollapsible = true,
    this.height,
    this.width,
    this.configuration,
    this.useAvailableWidth = true,
    this.useAvailableHeight = true,
    this.backgroundColor = 0xFF000000,
    this.textColor = 0xFFFFFFFF,
  });

  String getLocalizedTitle(String languageCode) {
    return titles[languageCode] ?? titles['en'] ?? '';
  }

  factory DynamicCard.fromJson(Map<String, dynamic> json) {
    return DynamicCard(
      id: json['Id'] ?? 0,
      titles: Map<String, String>.from(json['Titles'] ?? {}),
      order: json['Order'] ?? 0,
      type: CardType.values.firstWhere(
        (e) =>
            e.toString().toLowerCase() ==
            'cardtype.${json['Type']?.toLowerCase()}',
        orElse: () => CardType.Placeholder,
      ),
      isResizable: json['IsResizable'] ?? false,
      isCollapsible: json['IsCollapsible'] ?? true,
      height: json['Height'],
      width: json['Width'],
      configuration: json['Configuration'],
      useAvailableWidth: json['UseAvailableWidth'] ?? true,
      useAvailableHeight: json['UseAvailableHeight'] ?? true,
      backgroundColor: json['BackgroundColor'] ?? 0xFF000000,
      textColor: json['TextColor'] ?? 0xFFFFFFFF,
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'Id': id,
      'Titles': titles,
      'Order': order,
      'Type': type.toJson(),
      'IsResizable': isResizable,
      'IsCollapsible': isCollapsible,
      'Height': height,
      'Width': width,
      'Configuration': configuration,
      'UseAvailableWidth': useAvailableWidth,
      'UseAvailableHeight': useAvailableHeight,
      'BackgroundColor': backgroundColor,
      'TextColor': textColor,
    };
  }

  DynamicCard copyWith({
    int? id,
    Map<String, String>? titles,
    int? order,
    CardType? type,
    bool? isResizable,
    bool? isCollapsible,
    double? height,
    double? width,
    Map<String, dynamic>? configuration,
    bool? useAvailableWidth,
    bool? useAvailableHeight,
    int? backgroundColor,
    int? textColor,
  }) {
    return DynamicCard(
      id: id ?? this.id,
      titles: titles ?? this.titles,
      order: order ?? this.order,
      type: type ?? this.type,
      isResizable: isResizable ?? this.isResizable,
      isCollapsible: isCollapsible ?? this.isCollapsible,
      height: height ?? this.height,
      width: width ?? this.width,
      configuration: configuration ?? this.configuration,
      useAvailableWidth: useAvailableWidth ?? this.useAvailableWidth,
      useAvailableHeight: useAvailableHeight ?? this.useAvailableHeight,
      backgroundColor: backgroundColor ?? this.backgroundColor,
      textColor: textColor ?? this.textColor,
    );
  }
}
