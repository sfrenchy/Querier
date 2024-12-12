class DynamicCard {
  final int id;
  final String title;
  final int order;
  final String type;
  final bool isResizable;
  final bool isCollapsible;
  final double? height;
  final double? width;
  final Map<String, dynamic>? configuration;

  DynamicCard({
    required this.id,
    required this.title,
    required this.order,
    required this.type,
    this.isResizable = false,
    this.isCollapsible = true,
    this.height,
    this.width,
    this.configuration,
  });

  factory DynamicCard.fromJson(Map<String, dynamic> json) {
    print('DynamicCard.fromJson input: $json');
    try {
      final card = DynamicCard(
        id: json['Id'] ?? 0,
        title: json['Title'] ?? '',
        order: json['Order'] ?? 0,
        type: json['Type'] ?? '',
        isResizable: json['IsResizable'] ?? false,
        isCollapsible: json['IsCollapsible'] ?? false,
        height: json['Height']?.toDouble(),
        width: json['Width']?.toDouble(),
        configuration: json['Configuration'] as Map<String, dynamic>?,
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
      'Title': title,
      'Order': order,
      'Type': type,
      'IsResizable': isResizable,
      'IsCollapsible': isCollapsible,
      'Height': height,
      'Width': width,
      'Configuration': configuration,
    };
  }

  DynamicCard copyWith({
    int? id,
    String? title,
    int? order,
    String? type,
    bool? isResizable,
    bool? isCollapsible,
    double? height,
    double? width,
    Map<String, dynamic>? configuration,
  }) {
    return DynamicCard(
      id: id ?? this.id,
      title: title ?? this.title,
      order: order ?? this.order,
      type: type ?? this.type,
      isResizable: isResizable ?? this.isResizable,
      isCollapsible: isCollapsible ?? this.isCollapsible,
      height: height ?? this.height,
      width: width ?? this.width,
      configuration: configuration ?? this.configuration,
    );
  }
}
