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
    this.useAvailableWidth = false,
    this.useAvailableHeight = false,
  });

  String getLocalizedTitle(String languageCode) {
    return titles[languageCode] ?? titles['en'] ?? '';
  }

  factory DynamicCard.fromJson(Map<String, dynamic> json) {
    print('DynamicCard.fromJson input: $json');
    try {
      final card = DynamicCard(
        id: json['Id'] ?? 0,
        titles: Map<String, String>.from(json['Titles'] ?? {}),
        order: json['Order'] ?? 0,
        type: CardType.values.firstWhere(
          (e) => e.toString().split('.').last == json['Type'],
          orElse: () => CardType.Custom,
        ),
        isResizable: json['IsResizable'] ?? false,
        isCollapsible: json['IsCollapsible'] ?? false,
        height: json['Height']?.toDouble(),
        width: json['Width']?.toDouble(),
        configuration: json['Configuration'] as Map<String, dynamic>?,
        useAvailableWidth: json['UseAvailableWidth'] ?? false,
        useAvailableHeight: json['UseAvailableHeight'] ?? false,
      );
      print('DynamicCard created successfully: $card');
      return card;
    } catch (e, stackTrace) {
      print('Error in DynamicCard.fromJson: $e');
      print('Stack trace: $stackTrace');
      rethrow;
    }
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
    );
  }
}
